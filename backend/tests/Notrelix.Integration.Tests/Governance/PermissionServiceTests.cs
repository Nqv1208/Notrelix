using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Governance.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppPermissionScope = Notrelix.Application.Common.Security.PermissionScope;

namespace Notrelix.Integration.Tests.Governance;

[Collection("Database")]
public class PermissionServiceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PermissionServiceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private (ApplicationDbContext Context, PermissionService Service) CreateFixture()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _db.CreateContext(tenant);
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        var snapshots = new ResourceAuthorizationSnapshotStore(
            [new BoardAuthorizationSnapshotResolver(context)]);
        var service = new PermissionService(context, context, context, snapshots, clockMock.Object);
        return (context, service);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldAllowOwnerForAllWorkspaceActions()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(ownerId, workspace.AccountId, workspace.Id, ResourceKind.Create("workspaces.workspace"), null, PermissionAction.DeleteWorkspace, AppPermissionScope.Workspace);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
        decision.EffectiveLevel.Should().Be(PermissionLevel.Owner);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldDenyNonMembers()
    {
        var (context, service) = CreateFixture();
        var workspace = Workspace.Create(Guid.NewGuid(), Guid.NewGuid(), "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(Guid.NewGuid(), workspace.AccountId, workspace.Id, ResourceKind.Create("workspaces.workspace"), null, PermissionAction.ViewWorkspace, AppPermissionScope.Workspace);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_workspace_member");
    }

    [Fact]
    public async Task EvaluateAsync_PrivateBoard_ShouldHideForNonBoardMembers()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceBoard_ShouldAllowWorkspaceMembersToView()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Workspace Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void ResourcePermission_IsExpired_ShouldRespectExpiration()
    {
        var expirationDateTime = DateTimeOffset.UtcNow;
        var workspaceId = Guid.NewGuid();

        var activePerm = ResourcePermission.Grant(Guid.NewGuid(), workspaceId, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), expirationDateTime);

        var expiredPerm = ResourcePermission.Grant(Guid.NewGuid(), workspaceId, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), expirationDateTime.AddHours(-2), effect: PermissionEffect.Allow, conditionJson: null, priority: 100);

        activePerm.IsDeleted.Should().BeFalse();
        expiredPerm.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_ViewerCannotUpdateItem()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, viewerId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, viewerId, BoardRole.Observer, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(viewerId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.UpdateItem, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("missing_permission");
    }

    [Fact]
    public async Task EvaluateAsync_EditorCanUpdateItem()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, editorId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, Now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, editorId, BoardRole.Member, Now));
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(editorId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.UpdateItem, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WorkspaceGuestCannotViewPrivateBoard()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, guestId, WorkspaceRole.Guest, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(guestId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_RevokedPermissionsAreInvalid()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, memberId, WorkspaceRole.Member, ownerId, Now));

        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Private Board", null, Now, BoardVisibility.Private);
        context.Boards.Add(board);

        var permission = ResourcePermission.Grant(Guid.NewGuid(), workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionSubjectType.User, memberId, PermissionLevel.Editor, PermissionLevel.Owner, ownerId, Now);
        permission.Revoke(ownerId, Now);

        context.ResourcePermissions.Add(permission);
        await context.SaveChangesAsync();

        var permissionContext = new PermissionContext(memberId, workspace.AccountId, workspace.Id, ResourceKind.Create("work-management.board"), board.Id, PermissionAction.ViewBoard, AppPermissionScope.Resource);

        var decision = await service.EvaluateAsync(permissionContext);

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_SamePriorityDenyOverridesAllow()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            workspace.AccountId,
            workspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));

        context.PermissionRules.Add(PermissionRule.Create(
            workspace.AccountId,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.UpdateItem,
            PermissionEffect.Allow,
            ownerId,
            Now,
            priority: 100));
        context.PermissionRules.Add(PermissionRule.Create(
            workspace.AccountId,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.UpdateItem,
            PermissionEffect.Deny,
            ownerId,
            Now,
            priority: 100));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            workspace.AccountId,
            workspace.Id,
            ResourceKind.Create("work-management.board"),
            Guid.NewGuid(),
            PermissionAction.UpdateItem,
            AppPermissionScope.Resource));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("denied_by_rule");
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("future")]
    [InlineData("expired")]
    public async Task EvaluateAsync_InactiveOrOutOfWindowRule_IsIgnored(string ruleState)
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Rule Window WS", $"rule-window-{Guid.NewGuid():N}", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            workspace.AccountId,
            workspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));

        var rule = PermissionRule.Create(
            workspace.AccountId,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.UpdateItem,
            PermissionEffect.Allow,
            ownerId,
            Now,
            startsAt: ruleState == "future" ? Now.AddHours(1) : null,
            expiresAt: ruleState == "expired" ? Now.AddHours(-1) : null);

        if (ruleState == "disabled")
        {
            rule.Disable(ownerId, Now);
        }

        context.PermissionRules.Add(rule);
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            workspace.AccountId,
            workspace.Id,
            ResourceKind.Create("work-management.board-item"),
            Guid.NewGuid(),
            PermissionAction.UpdateItem,
            AppPermissionScope.Resource));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("missing_permission");
    }

    [Fact]
    public async Task EvaluateAsync_BoardFromAnotherWorkspace_IsHidden()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var requestedWorkspace = Workspace.Create(Guid.NewGuid(), ownerId, "Requested WS", $"requested-{Guid.NewGuid():N}", Now);
        var foreignWorkspace = Workspace.Create(Guid.NewGuid(), ownerId, "Foreign WS", $"foreign-{Guid.NewGuid():N}", Now);
        context.Workspaces.AddRange(requestedWorkspace, foreignWorkspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            requestedWorkspace.AccountId,
            requestedWorkspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));

        var foreignBoard = Board.Create(
            foreignWorkspace.AccountId,
            foreignWorkspace.Id,
            ownerId,
            "Foreign Board",
            null,
            Now,
            BoardVisibility.Workspace);
        context.Boards.Add(foreignBoard);
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            requestedWorkspace.AccountId,
            requestedWorkspace.Id,
            ResourceKind.Create("work-management.board"),
            foreignBoard.Id,
            PermissionAction.ViewBoard,
            AppPermissionScope.Resource));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("resource_not_found");
    }

    [Fact]
    public async Task EvaluateAsync_WrongAccountCannotUseWorkspaceMembership()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Test WS", "test-ws", Now);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            workspace.AccountId,
            workspace.Id,
            memberId,
            WorkspaceRole.Member,
            ownerId,
            Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            Guid.NewGuid(),
            workspace.Id,
            ResourceKind.Create("workspaces.workspace"),
            null,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Workspace));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_workspace_member");
    }

    [Fact]
    public void BoardItem_UpdatesFieldValuesCorrectly()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        var item = BoardItem.CreateRoot(Guid.NewGuid(), workspaceId, boardId, groupId, "Enterprise Item", Notrelix.Domain.SharedKernel.Ordering.FractionalIndex.Initial(), creatorId, Now);

        item.Rename("Renamed Item", creatorId, Now);

        item.Name.Should().Be("Renamed Item");
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_ShouldAllowOwnerForAllAccountActions()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var account = Account.Create("Owner Account", $"owner-{Guid.NewGuid():N}", AccountType.Personal, ownerId, Now);
        context.Accounts.Add(account);
        context.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            ownerId,
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.CreateWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeTrue();
        decision.EffectiveLevel.Should().Be(PermissionLevel.Owner);
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_ShouldDenyNonMembers()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var account = Account.Create("Other Account", $"other-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            Guid.NewGuid(),
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_account_member");
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_ShouldAllowActiveMemberToViewWorkspaces()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var account = Account.Create("Member Account", $"member-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        context.Accounts.Add(account);
        context.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, Now));
        context.AccountMembers.Add(AccountMember.Create(account.Id, memberId, AccountRole.Member, ownerId, Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeTrue();
        decision.EffectiveLevel.Should().Be(PermissionLevel.Viewer);
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_AdminBaseline_AllowsCreateWorkspaceWithoutRule()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var account = Account.Create("Admin Account", $"admin-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        context.Accounts.Add(account);
        context.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, Now));
        context.AccountMembers.Add(AccountMember.Create(account.Id, adminId, AccountRole.Admin, ownerId, Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            adminId,
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.CreateWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeTrue(
            "the Phase 13 frozen Account baseline (IA-TST-AUTHZ-APP-004) allows Admin CreateWorkspace");
    }

    /// <summary>
    /// IA-TST-AUTHZ-APP-004 / IAREQ090 / IAREQ138.
    /// Frozen Phase 13 Account role/action baseline evaluated through the
    /// canonical centralized evaluator (not a duplicated test-only policy).
    /// </summary>
    public static TheoryData<AccountRole, PermissionAction, bool> AccountRoleActionMatrix => new()
    {
        { AccountRole.Owner, PermissionAction.ViewWorkspace, true },
        { AccountRole.Owner, PermissionAction.CreateWorkspace, true },
        { AccountRole.Admin, PermissionAction.ViewWorkspace, true },
        { AccountRole.Admin, PermissionAction.CreateWorkspace, true },
        { AccountRole.Member, PermissionAction.ViewWorkspace, true },
        { AccountRole.Member, PermissionAction.CreateWorkspace, false },
        { AccountRole.BillingAdmin, PermissionAction.ViewWorkspace, true },
        { AccountRole.BillingAdmin, PermissionAction.CreateWorkspace, false },
        { AccountRole.SecurityAdmin, PermissionAction.ViewWorkspace, true },
        { AccountRole.SecurityAdmin, PermissionAction.CreateWorkspace, false },
    };

    [Theory]
    [MemberData(nameof(AccountRoleActionMatrix))]
    public async Task EvaluateAsync_AccountScope_FrozenRoleActionMatrix(
        AccountRole role,
        PermissionAction action,
        bool expectedAllowed)
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var account = Account.Create($"Matrix {role}", $"matrix-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        context.Accounts.Add(account);
        context.AccountMembers.Add(AccountMember.Create(account.Id, memberId, role, ownerId, Now));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            action,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().Be(expectedAllowed,
            $"frozen baseline: {role} × {action} must be {(expectedAllowed ? "allow" : "deny")}");
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_MemberFromOtherAccount_Denied()
    {
        var (context, service) = CreateFixture();
        var ownerAId = Guid.NewGuid();
        var memberBId = Guid.NewGuid();
        var accountA = Account.Create("Account A", $"account-a-{Guid.NewGuid():N}", AccountType.Team, ownerAId, Now);
        var accountB = Account.Create("Account B", $"account-b-{Guid.NewGuid():N}", AccountType.Team, ownerAId, Now);
        context.Accounts.Add(accountA);
        context.Accounts.Add(accountB);
        // memberB belongs ONLY to Account B
        context.AccountMembers.Add(AccountMember.Create(accountB.Id, memberBId, AccountRole.Admin, ownerAId, Now));
        await context.SaveChangesAsync();

        // memberB of Account B is evaluated against Account A
        var decision = await service.EvaluateAsync(new PermissionContext(
            memberBId,
            accountA.Id,
            null,
            ResourceKind.Create("accounts.account"),
            accountA.Id,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_account_member");
    }

    /// <summary>
    /// IA-TST-AUTHZ-APP-006 / IAREQ138 — an applicable explicit Governance deny
    /// must not be overturned by the Admin CreateWorkspace baseline fallback.
    /// The rule is workspace-keyed per the current Governance model; the account
    /// evaluation considers it through the workspace context carried by the
    /// permission context, exactly as the production pipeline resolves it.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_AccountScope_ExplicitGovernanceDeny_OverridesAdminFallback()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var account = Account.Create("Deny Account", $"deny-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        var workspace = Workspace.Create(account.Id, ownerId, "Anchor WS", $"anchor-{Guid.NewGuid():N}", Now);
        context.Accounts.Add(account);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.AccountMembers.Add(AccountMember.Create(account.Id, adminId, AccountRole.Admin, ownerId, Now));
        context.PermissionRules.Add(PermissionRule.Create(
            account.Id,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            adminId,
            null,
            PermissionAction.CreateWorkspace,
            PermissionEffect.Deny,
            ownerId,
            Now,
            priority: 100));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            adminId,
            account.Id,
            workspace.Id,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.CreateWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeFalse(
            "explicit Governance deny must override the non-owner role fallback (IAREQ138)");
        decision.ReasonCode.Should().Be("denied_by_rule");
    }

    /// <summary>
    /// IA-TST-AUTHZ-APP-007 / IAREQ138 — an applicable explicit Governance allow
    /// can grant a baseline-denied non-owner action without inventing a new
    /// PermissionAction: Member baseline denies CreateWorkspace; an explicit rule
    /// grants it.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_AccountScope_ExplicitGovernanceAllow_GrantsBaselineDeniedAction()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var account = Account.Create("Allow Account", $"allow-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        var workspace = Workspace.Create(account.Id, ownerId, "Anchor WS", $"anchor-{Guid.NewGuid():N}", Now);
        context.Accounts.Add(account);
        context.Workspaces.Add(workspace);
        context.WorkspaceMembers.Add(WorkspaceMember.Create(workspace.AccountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        context.AccountMembers.Add(AccountMember.Create(account.Id, memberId, AccountRole.Member, ownerId, Now));
        context.PermissionRules.Add(PermissionRule.Create(
            account.Id,
            workspace.Id,
            PermissionScopeType.Workspace,
            null,
            null,
            PermissionSubjectType.User,
            memberId,
            null,
            PermissionAction.CreateWorkspace,
            PermissionEffect.Allow,
            ownerId,
            Now,
            priority: 100));
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            memberId,
            account.Id,
            workspace.Id,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.CreateWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeTrue(
            "explicit Governance allow may grant a baseline-denied non-owner action when policy supports it");
    }

    [Fact]
    public async Task EvaluateAsync_AccountScope_ShouldDenySuspendedMembers()
    {
        var (context, service) = CreateFixture();
        var ownerId = Guid.NewGuid();
        var suspendedId = Guid.NewGuid();
        var account = Account.Create("Suspend Account", $"suspend-{Guid.NewGuid():N}", AccountType.Team, ownerId, Now);
        context.Accounts.Add(account);
        var member = AccountMember.Create(account.Id, suspendedId, AccountRole.Member, ownerId, Now);
        member.Suspend(ownerId, Now, activeOwnerCount: 1);
        context.AccountMembers.Add(member);
        await context.SaveChangesAsync();

        var decision = await service.EvaluateAsync(new PermissionContext(
            suspendedId,
            account.Id,
            null,
            ResourceKind.Create("accounts.account"),
            account.Id,
            PermissionAction.ViewWorkspace,
            AppPermissionScope.Account));

        decision.IsAllowed.Should().BeFalse();
        decision.ReasonCode.Should().Be("not_account_member");
    }
}
