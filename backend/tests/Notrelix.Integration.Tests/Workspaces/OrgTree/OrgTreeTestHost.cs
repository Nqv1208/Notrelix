using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Infrastructure.Workspaces.Spaces;
using Notrelix.Infrastructure.CrossContext.Workspaces.Documents;
using Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Features.WorkManagement.Public.SpacePlacement;
using Notrelix.Application.Features.Documents.Public.SpacePlacement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>A seeded Workspace: its Account, the Workspace and its Owner.</summary>
internal sealed record OrgStack(Guid AccountId, Guid WorkspaceId, Guid OwnerId, string Slug);

/// <summary>
/// Production pipeline slice for the org-tree suites (Space hierarchy,
/// space-scoped authorization, collection visibility, placement) on PostgreSQL:
/// execution context, data session transaction (expected-version constraint),
/// the one access-control stage with the real facts provider and engine,
/// idempotency, and every Application handler registered by assembly scan.
/// Seeding helpers state roles and grants explicitly (no hidden privilege).
/// </summary>
internal sealed class OrgTreeTestHost
{
    public static readonly DateTimeOffset BaseTime = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;

    public OrgTreeTestHost(PostgresTestContainer db)
    {
        _db = db;
    }

    public DateTimeOffset Now { get; set; } = BaseTime;

    public PostgresTestContainer Database => _db;

    // ── Pipeline ──────────────────────────────────────────────────────────

    public ServiceProvider CreateProvider(Guid accountId, Guid userId, Action<IServiceCollection>? configure = null)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(() => Now);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetBoardsQuery).Assembly));

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(GetBoardsQuery).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp => _db.CreateOwnerContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IPageAuthorizationFacts, PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
        services.AddScoped<IResourceVisibility>(sp =>
            new PostgresResourceVisibility(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<TimeProvider>()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IResourceCreatorGrant,
            Notrelix.Infrastructure.Data.Authz.ResourceCreatorGrantAdapter>();

        services.AddScoped<ISpaceStore>(sp => new SpaceStore(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<ISpaceTargetAuthorizer, SpaceTargetAuthorizer>();
        services.AddScoped<ISpaceContentReader, SpaceContentReader>();
        services.AddScoped<IBoardSpacePlacementFacts, BoardSpacePlacementFactsAdapter>();
        services.AddScoped<IPageSpacePlacementFacts, PageSpacePlacementFactsAdapter>();
        services.AddScoped<ISpaceReachReader, SpaceReachReader>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Public.GettingStarted.IFirstWorkItemFacts, Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement.FirstWorkItemFactsAdapter>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Public.BoardWork.IBoardWorkSummaryFacts, Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement.BoardWorkSummaryFactsAdapter>();
        services.AddScoped<Notrelix.Application.Features.Documents.Public.SpaceOverview.IPageSpaceOverviewFacts, Notrelix.Infrastructure.CrossContext.Workspaces.Documents.PageSpaceOverviewFactsAdapter>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Public.SpacePlacement.ISpacePlacementTargets,
            Notrelix.Infrastructure.Workspaces.Spaces.SpacePlacementTargets>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement,
            Notrelix.Application.Features.WorkManagement.Boards.Services.BoardSpacePlacement>();
        services.AddScoped<Notrelix.Application.Features.Documents.Pages.Services.IPageSpacePlacement,
            Notrelix.Application.Features.Documents.Pages.Services.PageSpacePlacement>();
        services.AddScoped<Notrelix.Application.Features.Documents.Pages.Services.IPageSubtreeAuthorizer,
            Notrelix.Application.Features.Documents.Pages.Services.PageSubtreeAuthorizer>();
        services.AddScoped<Notrelix.Application.Features.Documents.Pages.Services.IPageSubtreeStore,
            Notrelix.Infrastructure.Documents.Pages.PageSubtreeStore>();

        services.AddScoped<Notrelix.Application.Features.Identity.Public.Queries.IIdentityUserFacts, Notrelix.Application.Features.Identity.Users.Services.IdentityUserFactsProvider>();
        services.AddScoped<Notrelix.Application.Features.Accounts.Public.Domains.IAccountDomainFacts, Notrelix.Application.Features.Accounts.Members.AccountDomainFactsProvider>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Public.Members.IMemberEmailDisclosure, Notrelix.Application.Features.Workspaces.Members.Services.MemberEmailDisclosure>();
        services.AddScoped<IResourceAccessChangeReader, Notrelix.Infrastructure.Data.Authz.EfResourceAccessChangeReader>();
        services.AddScoped<Notrelix.Application.Features.Governance.Public.Access.IResourceAccessChangeRecorder, Notrelix.Application.Features.Governance.AccessChanges.ResourceAccessChangeRecorder>();
        services.AddScoped<Notrelix.Application.Features.WorkManagement.Boards.Services.BoardAccessChangeLog>();
        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<IActorLookupService, ActorLookupService>();
        services.AddScoped<IMemberPhoneLookup, Notrelix.Infrastructure.Identity.Profiles.MemberPhoneLookup>();
        services.AddScoped<IWorkspaceAccessResolver, WorkspaceAccessResolver>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp => sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp => sp.GetRequiredService<IdempotencyExecutionContext>());

        configure?.Invoke(services);
        services.AddProductionCrossContextAdapters();
        return services.BuildServiceProvider();
    }

    /// <summary>Sends through the pipeline; a failed <see cref="Result"/> fails the test.</summary>
    public static async Task<object?> SendAsync(ServiceProvider provider, object request)
    {
        using var scope = provider.CreateScope();
        // Idempotent requests (creates) need the key a transport would bind; others ignore it.
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set($"org-tree-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        if (result is Result plain)
            plain.Succeeded.Should().BeTrue(string.Join("; ", plain.Errors));
        return result;
    }

    /// <summary>
    /// Sends an <c>IIdempotentRequest</c> with a fresh execution key bound in the
    /// scope, as the HTTP transport does (a failed Result fails the test).
    /// </summary>
    public static async Task<object?> SendIdempotentAsync(ServiceProvider provider, object request, string? key = null)
    {
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set(key ?? $"org-tree-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        if (result is Result plain)
            plain.Succeeded.Should().BeTrue(string.Join("; ", plain.Errors));
        return result;
    }

    public static async Task<T> QueryAsync<T>(ServiceProvider provider, IRequest<Result<T>> request)
    {
        var result = (Result<T>)(await SendAsync(provider, request))!;
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors));
        return result.Data!;
    }

    // ── Seeding ───────────────────────────────────────────────────────────

    public async Task WithSystemContextAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await action(context);
        await context.SaveChangesAsync();
    }

    public async Task<T> ReadAsync<T>(Func<ApplicationDbContext, Task<T>> read)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await read(context);
    }

    public async Task<OrgStack> SeedWorkspaceAsync()
    {
        var owner = NewUser("Owner");
        var account = Account.Create("Org Account", $"org-{Guid.NewGuid():N}", AccountType.Team, owner.Id, BaseTime);
        var slug = $"org-ws-{Guid.NewGuid():N}";
        var workspace = Workspace.Create(account.Id, owner.Id, "Org Workspace", slug, BaseTime);

        await WithSystemContextAsync(seed =>
        {
            seed.Users.Add(owner);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, BaseTime));
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, BaseTime));
            return Task.CompletedTask;
        });
        await SyncGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner);

        return new OrgStack(account.Id, workspace.Id, owner.Id, workspace.Slug);
    }

    /// <summary>Adds an active Workspace member with exactly the stated role.</summary>
    public async Task<Guid> AddMemberAsync(OrgStack stack, WorkspaceRole role, string name = "Member")
    {
        var user = NewUser(name);
        await WithSystemContextAsync(seed =>
        {
            seed.Users.Add(user);
            seed.AccountMembers.Add(AccountMember.Create(stack.AccountId, user.Id, AccountRole.Member, stack.OwnerId, BaseTime));
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(stack.AccountId, stack.WorkspaceId, user.Id, role, stack.OwnerId, BaseTime));
            return Task.CompletedTask;
        });
        await SyncGrantAsync(stack.AccountId, stack.WorkspaceId, user.Id, role);
        return user.Id;
    }

    /// <summary>An authenticated user of the Account who is not a member of the Workspace.</summary>
    public async Task<Guid> AddOutsiderAsync(OrgStack stack)
    {
        var user = NewUser("Outsider");
        await WithSystemContextAsync(seed =>
        {
            seed.Users.Add(user);
            seed.AccountMembers.Add(AccountMember.Create(stack.AccountId, user.Id, AccountRole.Member, stack.OwnerId, BaseTime));
            return Task.CompletedTask;
        });
        return user.Id;
    }

    public async Task<Guid> SeedBoardAsync(
        OrgStack stack,
        string title,
        BoardVisibility visibility = BoardVisibility.Workspace,
        Guid? spaceId = null,
        Action<Board>? transition = null)
    {
        var board = Board.Create(stack.AccountId, stack.WorkspaceId, stack.OwnerId, title, null, BaseTime, visibility, spaceId: spaceId);
        transition?.Invoke(board);
        await WithSystemContextAsync(seed =>
        {
            seed.Boards.Add(board);
            return Task.CompletedTask;
        });
        return board.Id;
    }

    public async Task<Guid> SeedPageAsync(OrgStack stack, string title, Guid? parentId = null, Action<Page>? transition = null)
    {
        var page = Page.Create(stack.AccountId, stack.WorkspaceId, title, stack.OwnerId, BaseTime, parentId);
        transition?.Invoke(page);
        await WithSystemContextAsync(seed =>
        {
            seed.Pages.Add(page);
            return Task.CompletedTask;
        });
        return page.Id;
    }

    public Task SeedBoardMemberAsync(Guid boardId, Guid userId, BoardRole role) =>
        WithSystemContextAsync(seed =>
        {
            seed.BoardMembers.Add(BoardMember.Create(boardId, userId, role, BaseTime));
            return Task.CompletedTask;
        });

    public Task SeedResourcePermissionAsync(
        OrgStack stack,
        string resourceKind,
        Guid resourceId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        PermissionLevel level) =>
        WithSystemContextAsync(seed =>
        {
            seed.ResourcePermissions.Add(ResourcePermission.Grant(
                stack.AccountId, stack.WorkspaceId, ResourceKind.Create(resourceKind), resourceId,
                subjectType, subjectId, level, stack.OwnerId, BaseTime));
            return Task.CompletedTask;
        });

    public Task SeedRuleAsync(
        OrgStack stack,
        PermissionSubjectType subjectType,
        Guid subjectId,
        PermissionAction action,
        PermissionEffect effect,
        string? resourceKind = null,
        Guid? resourceId = null,
        int priority = 100) =>
        WithSystemContextAsync(seed =>
        {
            seed.PermissionRules.Add(PermissionRule.Create(
                stack.AccountId,
                stack.WorkspaceId,
                resourceId is null ? PermissionScopeType.Workspace : PermissionScopeType.Board,
                resourceKind is null ? null : ResourceKind.Create(resourceKind),
                resourceId,
                subjectType,
                subjectId,
                null,
                action,
                effect,
                stack.OwnerId,
                BaseTime,
                priority: priority));
            return Task.CompletedTask;
        });

    /// <summary>
    /// Seeds a Space directly (bypassing authorization) appended under its
    /// parent, with the closure rows the production store would stage. The
    /// creator becomes its first Space Admin (Domain rule).
    /// </summary>
    public async Task<Guid> SeedSpaceAsync(
        OrgStack stack,
        string name,
        Notrelix.Domain.Workspaces.Spaces.SpaceVisibility visibility,
        Guid createdBy,
        Guid? parentId = null,
        Notrelix.Domain.Workspaces.Spaces.SpaceType type = Notrelix.Domain.Workspaces.Spaces.SpaceType.Folder,
        Action<Notrelix.Domain.Workspaces.Spaces.Space>? transition = null)
    {
        Guid spaceId = Guid.Empty;
        await WithSystemContextAsync(async seed =>
        {
            var siblings = await seed.Spaces
                .Where(x => x.WorkspaceId == stack.WorkspaceId && x.ParentSpaceId == parentId)
                .Select(x => x.Position)
                .ToListAsync();
            var last = siblings.OrderBy(p => p).LastOrDefault();
            var position = FractionalIndexGenerator.GenerateKeyBetween(last, null);

            var ancestors = parentId is { } p
                ? await seed.SpacePaths.Where(x => x.DescendantId == p).Select(x => new { x.AncestorId, x.Depth }).ToListAsync()
                : [];
            var parentFact = parentId is { } pid
                ? new Notrelix.Domain.Workspaces.Spaces.SpaceParentFact(pid, stack.WorkspaceId, true, ancestors.OrderByDescending(a => a.Depth).Select(a => a.AncestorId).ToList())
                : null;

            var space = Notrelix.Domain.Workspaces.Spaces.Space.Create(
                stack.AccountId, stack.WorkspaceId, name, visibility, createdBy, BaseTime, type,
                placement: new Notrelix.Domain.Workspaces.Spaces.SpacePlacement(parentFact, position, []));
            transition?.Invoke(space);
            seed.Spaces.Add(space);
            seed.SpacePaths.Add(new Notrelix.Infrastructure.Data.Workspaces.SpacePathRecord(stack.AccountId, stack.WorkspaceId, space.Id, space.Id, 0));
            foreach (var ancestor in ancestors)
                seed.SpacePaths.Add(new Notrelix.Infrastructure.Data.Workspaces.SpacePathRecord(stack.AccountId, stack.WorkspaceId, ancestor.AncestorId, space.Id, ancestor.Depth + 1));
            spaceId = space.Id;
        });
        return spaceId;
    }

    /// <summary>Seeds an open, assigned Item on the board (group + item + assignment).</summary>
    public async Task<Guid> SeedAssignedItemAsync(OrgStack stack, Guid boardId, Guid assigneeId, string name)
    {
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, BaseTime);
        var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, name, FractionalIndex.Create("a0"), stack.OwnerId, BaseTime);
        await WithSystemContextAsync(seed =>
        {
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(item);
            seed.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, stack.WorkspaceId, boardId, item.Id, assigneeId, stack.OwnerId, BaseTime));
            return Task.CompletedTask;
        });
        return item.Id;
    }

    private async Task SyncGrantAsync(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        await using var grant = _db.CreateOwnerContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, userId, role, BaseTime, CancellationToken.None);
        await grant.SaveChangesAsync();
    }

    private static User NewUser(string name)
    {
        var user = User.Create($"org-{Guid.NewGuid():N}@example.com", $"Org {name}", "hashed", BaseTime, true);
        user.ConfirmEmail(user.Id, BaseTime);
        return user;
    }

    public static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
