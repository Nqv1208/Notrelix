using FluentValidation;
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
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidation = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Integration.Tests.Governance;

/// <summary>
/// TAC-WG-FLOW-04/05/06 — canonical authorization pipeline over real PostgreSQL.
/// Proves grant/revoke/get resource-permission mutations and reads, page-scoped
/// authorization, and canonical pipeline routing (allow/deny) through the one
/// AccessPolicyEngine — no second evaluator.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class GovernanceResourcePermissionFlowTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GovernanceResourcePermissionFlowTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 41D — GrantResourcePermission variants ──────────────────────────────

    [Fact]
    public async Task Grant_OnBoard_ByOwner_Allows_WhenSubjectOwnedByOwner()
    {
        var (accountId, ownerId, workspaceId, boardId, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", ownerId, "Editor"));

        result.Succeeded.Should().BeTrue();
        result.Data.Level.Should().Be("Editor");
        result.Data.ResourceKind.Should().Be("work-management.board");

        await AssertAuditRecordedAsync(workspaceId, ownerId, "GrantResourcePermission");
    }

    [Fact]
    public async Task Grant_OnBoard_ByNonOwnerWithNoResourceLevel_IsForbidden()
    {
        var (accountId, ownerId, _, boardId, memberId) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, memberId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", ownerId, "Viewer"));

        await act.Should().ThrowAsync<AppForbidden>(
            "a workspace member without explicit resource-level authority cannot grant permissions");
    }

    [Fact]
    public async Task Grant_OnBoard_CannotDowngrade_ExistingHigherTargetLevel()
    {
        var (accountId, ownerId, workspaceId, boardId, memberId) = await SeedBoardStackAsync();

        using var ownerProvider = CreateProvider(accountId, ownerId);
        var granted = await SendAsync<Result<ResourcePermissionDto>>(ownerProvider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", memberId, "Manager"));
        granted.Succeeded.Should().BeTrue();

        // Second actor with only Editor-level authority on the same board
        // attempts to replace the existing Manager-level row — denied by the
        // engine's grant ceiling (max(requested, existing target)).
        var editorId = Guid.NewGuid();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, editorId, WorkspaceRole.Member);
        await SeedResourcePermissionAsync(accountId, workspaceId, "work-management.board", boardId, editorId, PermissionLevel.Editor);

        using var editorProvider = CreateProvider(accountId, editorId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(editorProvider,
            new GrantResourcePermissionCommand("work-management.board", boardId, "User", memberId, "Viewer"));

        await act.Should().ThrowAsync<AppForbidden>(
            "grant ceiling = max(requested, existing target) must exceed the editor's authority");
    }

    [Fact]
    public async Task Grant_OnPage_ByOwner_Allows_AndUsesPageAction()
    {
        var (accountId, ownerId, workspaceId, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", ownerId, "Commenter"));

        result.Succeeded.Should().BeTrue("a guessed page permission action must not be required; ManagePagePermission authorizes the grant");
        result.Data.ResourceKind.Should().Be("documents.page");
        result.Data.Level.Should().Be("Commenter");
    }

    [Fact]
    public async Task Grant_OnPage_SameSubjectAndLevel_ReplacesWithoutDuplicate()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, memberId, WorkspaceRole.Member);

        using var provider = CreateProvider(accountId, ownerId);

        var first = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));
        first.Succeeded.Should().BeTrue();
        var second = await SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", memberId, "Viewer"));

        second.Succeeded.Should().BeTrue();
        second.Data.Id.Should().Be(first.Data.Id, "grant is a semantic upsert: same subject+level reuses the active row");
    }

    [Fact]
    public async Task Grant_OnPage_ExpiredDates_AreRejectedByValidation()
    {
        var (accountId, ownerId, _, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var act = () => SendAsync<Result<ResourcePermissionDto>>(provider,
            new GrantResourcePermissionCommand("documents.page", pageId, "User", ownerId, "Viewer", DateTime.UtcNow.AddDays(1)));

        await act.Should().ThrowAsync<AppValidation>(
            "expiration is not supported for resource permissions");
    }

    // ── 41E — GetResourcePermissions ─────────────────────────────────────────

    [Fact]
    public async Task Get_OnPage_ReturnsGovernanceStateOnly_InWorkspaceScope()
    {
        var (accountId, ownerId, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, memberId, WorkspaceRole.Member);
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", pageId, memberId, PermissionLevel.Commenter);

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue();
        result.Data.Should().ContainSingle(p => p.SubjectId == memberId && p.Level == "Commenter");
        result.Data.Should().NotContain(p => p.ResourceKind != "documents.page",
            "the query is Governance-owned state scoped to the exact resource");
    }

    [Fact]
    public async Task Get_OnPage_DoesNotReturnRowsFromAnotherResource()
    {
        var (accountId, ownerId, workspaceId, pageId, _) = await SeedPageStackAsync();
        var (_, _, _, otherPageId, _) = await SeedPageStackAsync();
        await SeedResourcePermissionAsync(accountId, workspaceId, "documents.page", otherPageId, ownerId, PermissionLevel.Viewer);

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty("permissions of another page must not leak into this resource's list");
    }

    [Fact]
    public async Task Get_AsWorkspaceMember_OnWorkspacePage_Allows()
    {
        var (accountId, _, workspaceId, pageId, memberId) = await SeedPageStackAsync();
        await SeedWorkspaceMemberAsync(accountId, workspaceId, memberId, WorkspaceRole.Member);

        using var provider = CreateProvider(accountId, memberId);
        var result = await SendAsync<Result<List<ResourcePermissionDto>>>(provider,
            new GetResourcePermissionsQuery("documents.page", pageId));

        result.Succeeded.Should().BeTrue(
            "a workspace page is visible to members; ManagePagePermission flows through the same page branch as boards");
    }

    // ── 41F — Canonical authorization pipeline routing ───────────────────────

    [Fact]
    public async Task MoveBoardItem_MemberWithWorkspaceRole_Allows()
    {
        var (accountId, ownerId, _, boardId, _) = await SeedBoardStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var itemId = await ResolveBoardItemIdAsync(boardId);
        var groupId = await ResolveOtherGroupIdAsync(boardId);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
                .Set($"gov-move-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);

            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new MoveBoardItemCommand(itemId, groupId, 1.0d));
        }
    }

    [Fact]
    public async Task CreatePage_NonMember_IsForbidden()
    {
        var (accountId, _, workspaceId, _, _) = await SeedPageStackAsync();
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Outsider Page", null));

        await act.Should().ThrowAsync<AppForbidden>(
            "a non-member has no workspace role, so the engine denies CreatePage");
    }

    [Fact]
    public async Task CreatePage_Owner_Allows()
    {
        var (accountId, ownerId, workspaceId, _, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<Guid>>(provider,
            new CreatePageCommand(workspaceId, "Owner Page", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateComment_OnPage_MemberWithWorkspaceRole_Allows()
    {
        var (accountId, ownerId, _, pageId, _) = await SeedPageStackAsync();

        using var provider = CreateProvider(accountId, ownerId);
        var result = await SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageId, "Hello page", null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateComment_OnPage_Outsider_IsForbidden()
    {
        var (accountId, _, _, pageId, _) = await SeedPageStackAsync();
        var outsider = Guid.NewGuid();

        using var provider = CreateProvider(accountId, outsider);
        var act = () => SendAsync<Result<Guid>>(provider,
            CreateCommentCommand.ForPage(pageId, "Hello page", null));

        await act.Should().ThrowAsync<AppForbidden>("page-scoped actions are denied for non-members");
    }

    // ── composition -----------------------------------------------------------

    private static async Task<T> SendAsync<T>(ServiceProvider provider, object request)
        where T : class
    {
        using var scope = provider.CreateScope();
        var response = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        return (response as T)!;
    }

    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
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

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(GrantResourcePermissionCommand).Assembly));
        services.AddTransient<IValidator<GrantResourcePermissionCommand>, GrantResourcePermissionCommandValidator>();
        services.AddTransient<IValidator<GetResourcePermissionsQuery>, GetResourcePermissionsQueryValidator>();
        services.AddTransient<IValidator<CreatePageCommand>, CreatePageCommandValidator>();
        services.AddTransient<IValidator<CreateCommentCommand>, CreateCommentCommandValidator>();

        // Canonical frozen pipeline, outermost-to-innermost order.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());

        services.AddScoped<IResourceLocator, ResourceLocator>();
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
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        // Handlers under test.
        services.AddScoped<
            IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>,
            GrantResourcePermissionCommandHandler>();
        services.AddScoped<
            IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>,
            GetResourcePermissionsQueryHandler>();
        services.AddScoped<
            IRequestHandler<CreatePageCommand, Result<Guid>>,
            CreatePageCommandHandler>();
        services.AddScoped<
            IRequestHandler<CreateCommentCommand, Result<Guid>>,
            CreateCommentCommandHandler>();
        services.AddScoped<MoveBoardItemUseCase>();
        services.AddScoped<
            IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>,
            MoveBoardItemCommandHandler>();

        return services.BuildServiceProvider();
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid BoardId, Guid MemberId)> SeedBoardStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("Gov Board Account", $"gov-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Gov Board WS", $"gov-board-{Guid.NewGuid():N}", now);
        var board = Board.Create(account.Id, workspace.Id, owner.Id, "Gov Board", null, now);
        var group = BoardGroup.Create(
            account.Id, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), owner.Id, now);
        var item = BoardItem.CreateRoot(
            account.Id, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), owner.Id, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id, board.Id, member.Id);
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId, Guid PageId, Guid MemberId)> SeedPageStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"gov-{Guid.NewGuid():N}@example.com", "Gov Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("Gov Page Account", $"gov-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Gov Page WS", $"gov-page-{Guid.NewGuid():N}", now);
        var page = Page.Create(account.Id, workspace.Id, "Gov Page", owner.Id, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.Pages.Add(page);
        await seed.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id, page.Id, member.Id);
    }

    private async Task SeedWorkspaceMemberAsync(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspaceId, userId, role, userId, DateTimeOffset.UtcNow));
        await seed.SaveChangesAsync();
    }

    private async Task SeedResourcePermissionAsync(
        Guid accountId, Guid workspaceId, string resourceKind, Guid resourceId, Guid subjectId, PermissionLevel level)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        seed.ResourcePermissions.Add(ResourcePermission.Grant(
            accountId, workspaceId,
            ResourceKind.Create(resourceKind),
            resourceId,
            PermissionSubjectType.User,
            subjectId,
            level,
            subjectId,
            DateTimeOffset.UtcNow));
        await seed.SaveChangesAsync();
    }

    private async Task AssertAuditRecordedAsync(Guid workspaceId, Guid actorId, string action)
    {
        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.EnterpriseAuditLogs.AnyAsync(a =>
            a.WorkspaceId == workspaceId && a.ActorUserId == actorId && a.Action == action))
            .Should().BeTrue($"an audit row for '{action}' must be committed");
    }

    private async Task<Guid> ResolveBoardItemIdAsync(Guid boardId)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var id = await context.BoardItems
            .Where(i => i.BoardId == boardId)
            .Select(i => i.Id)
            .FirstOrDefaultAsync();
        id.Should().NotBeEmpty($"expected a seeded item on board {boardId}");
        return id;
    }

    private async Task<Guid> ResolveOtherGroupIdAsync(Guid boardId)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var id = await context.BoardGroups
            .Where(g => g.BoardId == boardId)
            .Select(g => g.Id)
            .FirstOrDefaultAsync();
        id.Should().NotBeEmpty($"expected a seeded group on board {boardId}");
        return id;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}