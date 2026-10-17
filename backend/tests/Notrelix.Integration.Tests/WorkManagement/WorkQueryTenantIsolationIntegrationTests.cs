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
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardSchema.Queries.GetBoardSchema;
using Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
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

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// SEC-QUERY-001 (WM-V3-QUAL-001). P3 list/read queries run through the real
/// pipeline (execution context, data session with RLS session context, access
/// control) on PostgreSQL. A member of workspace A reads its own board; asking
/// for a board of workspace B in the same account is denied before any data is
/// read, for the schema, item list and checklist reads alike.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkQueryTenantIsolationIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;

    public WorkQueryTenantIsolationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Member_ReadsItsOwnBoard_ButNotABoardOfAnotherWorkspace()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        var (ownBoard, ownItem) = await SeedBoardAsync(accountId, workspaceId, ownerId, "Own");
        var foreignWorkspace = Workspace.Create(accountId, Guid.NewGuid(), "Foreign WS", $"wm-f-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Workspaces.Add(foreignWorkspace);
            await seed.SaveChangesAsync();
        }

        var (foreignBoard, foreignItem) = await SeedBoardAsync(accountId, foreignWorkspace.Id, ownerId, "Foreign");
        using var provider = CreateProvider(accountId, ownerId);

        (await SendAsync(provider, new GetBoardSchemaQuery(ownBoard))).Should().BeOfType<BoardSchemaDto>();
        (await SendAsync(provider, new GetBoardItemsQuery(ownBoard))).As<List<BoardItemSlimDto>>().Should().ContainSingle();
        (await SendAsync(provider, new GetChecklistsQuery(ownItem))).As<Result<List<ChecklistDto>>>().Succeeded.Should().BeTrue();

        foreach (var request in new object[] { new GetBoardSchemaQuery(foreignBoard), new GetBoardItemsQuery(foreignBoard), new GetChecklistsQuery(foreignItem) })
        {
            var act = () => SendAsync(provider, request);
            await act.Should().ThrowAsync<Exception>($"{request.GetType().Name} for a foreign workspace must be denied")
                .Where(e => e is Notrelix.Application.Common.Exceptions.ForbiddenException || e is NotFoundException);
        }
    }

    private static async Task<object?> SendAsync(ServiceProvider provider, object request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private async Task<(Guid Board, Guid Item)> SeedBoardAsync(Guid accountId, Guid workspaceId, Guid ownerId, string title)
    {
        var now = DateTimeOffset.UtcNow;
        var board = Board.Create(accountId, workspaceId, ownerId, title, null, now);
        var group = BoardGroup.Create(accountId, workspaceId, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(accountId, workspaceId, board.Id, group.Id, $"{title} task", FractionalIndex.Create("a0"), ownerId, now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();
        return (board.Id, item.Id);
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

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

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
            RequestDescriptorRegistry.Create(typeof(GetBoardSchemaQuery).Assembly));

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
        services.AddScoped<
            IPageAuthorizationFacts,
            PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
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

        services.AddScoped<IRequestHandler<GetBoardSchemaQuery, BoardSchemaDto>, GetBoardSchemaQueryHandler>();
        services.AddScoped<IRequestHandler<GetBoardItemsQuery, List<BoardItemSlimDto>>, GetBoardItemsQueryHandler>();
        services.AddScoped<IRequestHandler<GetChecklistsQuery, Result<List<ChecklistDto>>>, GetChecklistsQueryHandler>();

        return services.BuildServiceProvider();
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wm-{Guid.NewGuid():N}@example.com", "WM Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WM Account", $"wm-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WM Workspace", $"wm-ws-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Users.Add(member);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
