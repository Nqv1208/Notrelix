using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.ReadPorts.WorkManagement;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using ExecutionContextClass = Notrelix.Application.Common.Context.ExecutionContext;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// IA-BRD-HANDSHAKE (PR-WG-07, phase 11 P3-B): production-graph proof that Board
/// authorization flows through the canonical Governance path via the WorkManagement-owned
/// transport-neutral resource facts SPI. The composed production pipeline
/// (ExecutionContextBehavior -> DataSessionBehavior -> AccessControlBehavior over real
/// PostgreSQL) routes the Board resource through ResourceLocator and resolves its
/// existence/audience/member-role facts from the WorkManagement adapter, then the pure
/// AccessPolicyEngine makes the Allow/Forbidden/NotFound decision — all on real data.
/// </summary>
[Collection("Database")]
public class BoardHandshakeAuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardHandshakeAuthorizationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ArchiveBoard_WorkspaceBoardOwner_AllowedThroughHandshake_AndCommitted()
    {
        // Actor is a Workspace owner AND the Board owner (BoardRole.Owner): the SPI
        // surfaces member role 'Owner', the engine grants ManageBoard, and the
        // handler archive commits atomically through the composed pipeline.
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"board-owner-{Guid.NewGuid():N}@example.com", "Board Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        Guid accountId, workspaceId, boardId;
        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(owner);
            var account = Account.Create("Handshake A", $"handshake-a-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
            var workspace = Workspace.Create(account.Id, owner.Id, "WS Handshake A", $"hs-a-{Guid.NewGuid():N}", now);
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
            var board = Board.Create(account.Id, workspace.Id, owner.Id, "Owner Board", null, now, BoardVisibility.Workspace);
            seed.Boards.Add(board);
            seed.BoardMembers.Add(BoardMember.Create(board.Id, owner.Id, BoardRole.Owner, now));
            await seed.SaveChangesAsync();
            accountId = account.Id;
            workspaceId = workspace.Id;
            boardId = board.Id;
        }

        using var provider = BuildPipelineProvider(accountId, workspaceId, owner.Id);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ArchiveBoardCommand(boardId));
        result.Succeeded.Should().BeTrue(
            "a Board owner with Workspace membership must archive through the canonical Governance handshake");

        await using var verify = _db.CreateContext(SystemTenant());
        var archived = await verify.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        archived.Should().NotBeNull();
        archived!.IsArchived.Should().BeTrue("the allowed board archive must commit durable state");
    }

    [Fact]
    public async Task ArchiveBoard_WorkspaceMemberWithoutBoardAuthority_DeniedBeforeCommit()
    {
        var now = DateTimeOffset.UtcNow;
        var (accountId, workspaceId, ownerId, boardId, memberId) =
            await SeedWorkspaceBoardAsync(visibility: BoardVisibility.Workspace, boardMemberUserId: null);

        using var provider = BuildPipelineProvider(accountId, workspaceId, memberId);
        var sender = provider.GetRequiredService<ISender>();

        var act = () => sender.Send(new ArchiveBoardCommand(boardId));

        var assertion = await act.Should().ThrowAsync<AppForbidden>(
            "a Workspace member with no Board-level grant must be forbidden from ManageBoard by the engine");

        await using var verify = _db.CreateContext(SystemTenant());
        var board = await verify.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        board!.IsArchived.Should().BeFalse(
            "a denied ManageBoard must never reach the handler and must leave no durable side effect");
    }

    [Fact]
    public async Task ArchiveBoard_PrivateBoardNonMember_HiddenAsNotFound()
    {
        var now = DateTimeOffset.UtcNow;
        var (accountId, workspaceId, ownerId, boardId, memberId) =
            await SeedWorkspaceBoardAsync(visibility: BoardVisibility.Private, boardMemberUserId: null);

        using var provider = BuildPipelineProvider(accountId, workspaceId, memberId);
        var sender = provider.GetRequiredService<ISender>();

        var act = () => sender.Send(new ArchiveBoardCommand(boardId));

        var assertion = await act.Should().ThrowAsync<AppNotFound>(
            "a restricted/private board with no member role must be hidden as NotFound, never exposed");

        await using var verify = _db.CreateContext(SystemTenant());
        var board = await verify.Boards.FirstOrDefaultAsync(b => b.Id == boardId);
        board!.IsArchived.Should().BeFalse("a hidden board must never be mutated");
    }

    [Fact]
    public async Task ArchiveBoard_CrossTenantBoard_DeniedWithoutMutation()
    {
        // Actor is a member of Account A workspace W_A; the board belongs to
        // Account B workspace W_B. The handshake locates the board under B, applies
        // B's tenant scope, and the engine denies because the actor has no B
        // membership — no cross-tenant mutation occurs.
        var now = DateTimeOffset.UtcNow;
        var (accountA, workspaceA, ownerA, _ /*irrelevant board*/, crossTenantUserId) =
            await SeedWorkspaceBoardAsync(visibility: BoardVisibility.Workspace, boardMemberUserId: null);

        // Create a second account B + workspace B + board B owned by a B user.
        var ownerB = User.Create($"cross-owner-{Guid.NewGuid():N}@example.com", "Cross Owner", "hashed", now, true);
        ownerB.ConfirmEmail(ownerB.Id, now);
        Guid accountB, workspaceB, boardB;
        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(ownerB);
            var account = Account.Create("Handshake B", $"handshake-b-{Guid.NewGuid():N}", AccountType.Team, ownerB.Id, now);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerB.Id, AccountRole.Owner, ownerB.Id, now));
            var workspace = Workspace.Create(account.Id, ownerB.Id, "WS Handshake B", $"hs-b-{Guid.NewGuid():N}", now);
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, ownerB.Id, WorkspaceRole.Owner, ownerB.Id, now));
            var board = Board.Create(account.Id, workspace.Id, ownerB.Id, "Cross Board", null, now, BoardVisibility.Workspace);
            seed.Boards.Add(board);
            await seed.SaveChangesAsync();
            accountB = account.Id;
            workspaceB = workspace.Id;
            boardB = board.Id;
        }

        using var provider = BuildPipelineProvider(accountA, workspaceA, crossTenantUserId);
        var sender = provider.GetRequiredService<ISender>();

        var act = () => sender.Send(new ArchiveBoardCommand(boardB));

        var assertion = await act.Should().ThrowAsync<AppForbidden>(
            "a cross-tenant board must be denied by the Governance handshake before any effect");

        await using var verify = _db.CreateContext(SystemTenant());
        var crossBoard = await verify.Boards.FirstOrDefaultAsync(b => b.Id == boardB);
        crossBoard!.IsArchived.Should().BeFalse(
            "a cross-tenant board must never be mutated by a foreign-account actor");
    }

    /// <summary>
    /// Seeds an Account with an Owner, a Workspace, a Workspace member actor, and a
    /// Board. <paramref name="boardMemberUserId"/> selects which user is granted a
    /// Board member row; pass the workspace member userId for the owner/authorized case.
    /// </summary>
    private async Task<(Guid AccountId, Guid WorkspaceId, Guid OwnerId, Guid BoardId, Guid MemberId)>
        SeedWorkspaceBoardAsync(BoardVisibility visibility, Guid? boardMemberUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"owner-{Guid.NewGuid():N}@example.com", "Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"member-{Guid.NewGuid():N}@example.com", "Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);

        Guid accountId, workspaceId, boardId;
        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(owner);
            seed.Users.Add(member);

            var account = Account.Create("Handshake C", $"handshake-c-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
            seed.AccountMembers.Add(AccountMember.Create(account.Id, member.Id, AccountRole.Member, owner.Id, now));

            var workspace = Workspace.Create(account.Id, owner.Id, "WS Handshake C", $"hs-c-{Guid.NewGuid():N}", now);
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));

            var board = Board.Create(account.Id, workspace.Id, owner.Id, "Handshake Board", null, now, visibility);
            seed.Boards.Add(board);
            if (boardMemberUserId.HasValue)
            {
                seed.BoardMembers.Add(BoardMember.Create(board.Id, boardMemberUserId.Value, BoardRole.Member, now));
            }
            await seed.SaveChangesAsync();

            accountId = account.Id;
            workspaceId = workspace.Id;
            boardId = board.Id;
        }

        return (accountId, workspaceId, owner.Id, boardId, member.Id);
    }

    /// <summary>
    /// Composes the production authentication/authorization pipeline slice: real
    /// ExecutionContextBehavior (resource->workspace via the SPI-backed ResourceLocator),
    /// real DataSessionBehavior (transaction + RLS), real AccessControlBehavior over the
    /// real PostgresAccessFactsProvider + WorkManagement SPI adapter + pure engine,
    /// followed by the real command handler. No authorization decision is duplicated.
    /// </summary>
    private ServiceProvider BuildPipelineProvider(Guid accountId, Guid workspaceId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, userId);

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var environmentMock = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Testing");

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddOptions();
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(userMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(environmentMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(ArchiveBoardCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
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
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IResourceAuthorizationFactsProvider>()));
        services.AddScoped<IResourceAuthorizationFactsProvider>(sp =>
            new WorkManagementResourceAuthorizationFactsProvider(
                sp.GetRequiredService<IWorkManagementDbContext>()));
        services.AddScoped<IAccessGrantProjectionService>(sp =>
            new AccessGrantProjectionService(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();
        services.AddScoped<ExecutionContextClass>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<ExecutionContextClass>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<ExecutionContextClass>());

        services.AddTransient<
            IRequestHandler<ArchiveBoardCommand, Notrelix.Application.Common.Models.Result>,
            ArchiveBoardCommandHandler>();

        return services.BuildServiceProvider();
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
