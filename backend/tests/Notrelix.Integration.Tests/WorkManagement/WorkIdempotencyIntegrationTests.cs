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
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Infrastructure.Data.WorkManagement;
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
/// IDEMP-INT-001 / IDEMP-MISMATCH-001 (WM-V3-IDEMP-001, server side). A Work
/// mutation runs through the real pipeline (idempotency behavior, data session,
/// access control) and the real PostgreSQL idempotency store: the same key with
/// the same payload has one effect and replays the first result; the same key with
/// a different payload is rejected with no second effect; and a failed attempt
/// rolls back without completing the key, so a retry with that key executes.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkIdempotencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;

    public WorkIdempotencyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SameKeySamePayload_HasOneEffect_AndReplaysTheFirstResult()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        var boardId = await SeedBoardAsync(accountId, workspaceId, ownerId);
        using var provider = CreateProvider(accountId, ownerId);
        var key = $"wm-idem-{Guid.NewGuid():N}";

        var first = await SendWithKeyAsync(provider, key, new CreateBoardGroupCommand(boardId, "Review"));
        var replay = await SendWithKeyAsync(provider, key, new CreateBoardGroupCommand(boardId, "Review"));

        replay.Data.Should().Be(first.Data, "a replay returns the first committed result");
        (await GroupTitlesAsync(boardId)).Count(t => t == "Review").Should().Be(1, "one logical operation, one effect");
    }

    [Fact]
    public async Task SameKeyDifferentPayload_IsRejected_WithNoSecondEffect()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        var boardId = await SeedBoardAsync(accountId, workspaceId, ownerId);
        using var provider = CreateProvider(accountId, ownerId);
        var key = $"wm-idem-{Guid.NewGuid():N}";

        await SendWithKeyAsync(provider, key, new CreateBoardGroupCommand(boardId, "Review"));
        var mismatch = () => SendWithKeyAsync(provider, key, new CreateBoardGroupCommand(boardId, "Different"));

        await mismatch.Should().ThrowAsync<IdempotencyPayloadMismatchException>("reusing a key for a different payload is a client conflict");
        (await GroupTitlesAsync(boardId)).Should().NotContain("Different");
    }

    [Fact]
    public async Task FailedAttempt_RollsBackWithoutCompletingTheKey_SoTheSameKeyRetryExecutes()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        var boardId = await SeedBoardAsync(accountId, workspaceId, ownerId);
        var (first, second) = await SeedTwoGroupsAsync(accountId, workspaceId, boardId, ownerId);
        using var provider = CreateProvider(accountId, ownerId);
        var key = $"wm-idem-{Guid.NewGuid():N}";
        var appendAfterFirst = new CreateBoardGroupCommand(boardId, "Appended", PreviousSiblingId: first);

        var stale = () => SendWithKeyAsync(provider, key, appendAfterFirst);
        await stale.Should().ThrowAsync<ConflictException>("the first group is not last while the second exists");
        await DeleteGroupAsync(second);

        var retry = await SendWithKeyAsync(provider, key, appendAfterFirst);

        retry.Succeeded.Should().BeTrue("the rolled-back attempt never completed the idempotency identity");
        (await GroupTitlesAsync(boardId)).Should().Contain("Appended");
    }

    private static async Task<Result<Guid>> SendWithKeyAsync(ServiceProvider provider, string key, CreateBoardGroupCommand command)
    {
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>().Set(key, IdempotencyExecutionSource.Internal);
        return (Result<Guid>)(await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))!;
    }

    private async Task<Guid> SeedBoardAsync(Guid accountId, Guid workspaceId, Guid ownerId)
    {
        var board = Board.Create(accountId, workspaceId, ownerId, "Idempotency Board", null, DateTimeOffset.UtcNow);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Boards.Add(board);
        await seed.SaveChangesAsync();
        return board.Id;
    }

    private async Task<(Guid First, Guid Second)> SeedTwoGroupsAsync(Guid accountId, Guid workspaceId, Guid boardId, Guid ownerId)
    {
        var now = DateTimeOffset.UtcNow;
        var first = BoardGroup.Create(accountId, workspaceId, boardId, "First", Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var second = BoardGroup.Create(accountId, workspaceId, boardId, "Second", Color.Create("#808080"), FractionalIndex.Create("a1"), ownerId, now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.BoardGroups.AddRange(first, second);
        await seed.SaveChangesAsync();
        return (first.Id, second.Id);
    }

    private async Task DeleteGroupAsync(Guid groupId)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var group = await context.BoardGroups.SingleAsync(g => g.Id == groupId);
        group.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
    }

    private async Task<List<string>> GroupTitlesAsync(Guid boardId)
    {
        await using var verify = _db.CreateContext(SystemTenant());
        return await verify.BoardGroups.Where(g => g.BoardId == boardId).Select(g => g.Title).ToListAsync();
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
            RequestDescriptorRegistry.Create(typeof(CreateBoardGroupCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

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

        services.AddScoped<IWorkOrderingScopeLock>(sp => new PostgresWorkOrderingScopeLock(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<WorkPlacementResolver>();
        services.AddScoped<
            IRequestHandler<CreateBoardGroupCommand, Result<Guid>>,
            CreateBoardGroupCommandHandler>();

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
