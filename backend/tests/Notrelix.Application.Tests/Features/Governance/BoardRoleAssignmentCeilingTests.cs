using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Board roles carry levels (Owner/Admin → Manager), so assigning a role is a
/// grant: a caller may not add or change a board member to a role whose level
/// exceeds the caller's own effective level on the Board, and a caller below
/// Manager may not raise their own role. Managers and workspace Owners are
/// unaffected; roles at or below the caller's level keep working.
/// </summary>
public sealed class BoardRoleAssignmentCeilingTests
{
    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid Caller = Guid.Parse("00000000-0000-0000-0000-00000000ca11");
    private static readonly Guid Colleague = Guid.Parse("00000000-0000-0000-0000-0000000000c0");
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");

    private static readonly ExecutionContextSnapshot Context = new(
        Caller, Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    [Theory]
    [InlineData(BoardRole.Owner)]
    [InlineData(BoardRole.Admin)]
    public void Editor_CannotMakeThemselvesOrOthersOwnerOrAdmin(BoardRole role)
    {
        var editor = Facts("Member"); // Workspace default on an unplaced board → Editor

        Add(editor, Caller, role).Should().Be(AccessDecisionKind.Forbidden, "self-promotion to a Manager-level role");
        Add(editor, Colleague, role).Should().Be(AccessDecisionKind.Forbidden, "granting above the caller's own level");
        Change(editor, Colleague, role).Should().Be(AccessDecisionKind.Forbidden);
        Change(editor with { ResourceMemberRole = "Member" }, Caller, role).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Theory]
    [InlineData(BoardRole.Member)]
    [InlineData(BoardRole.Observer)]
    [InlineData(BoardRole.Guest)]
    public void Editor_KeepsAssigningRolesAtOrBelowTheirLevel_ToOthers(BoardRole role)
    {
        var editor = Facts("Member");

        Add(editor, Colleague, role).Should().Be(AccessDecisionKind.Allowed);
        Change(editor, Colleague, role).Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Observer_CannotRaiseTheirOwnRole_EvenWithinTheirLevel()
    {
        // Workspace default gives Editor, but the board role Observer is
        // read-only on items; changing it to Member would lift that limit.
        var observer = Facts("Member", boardRole: "Observer");

        Change(observer, Caller, BoardRole.Member).Should().Be(AccessDecisionKind.Forbidden);
        Change(observer, Caller, BoardRole.Observer).Should().Be(AccessDecisionKind.Allowed, "not a raise");
        Change(observer, Colleague, BoardRole.Member).Should().Be(AccessDecisionKind.Allowed, "others at or below the caller's level");
    }

    [Fact]
    public void NonMemberEditor_CannotAddThemselves()
    {
        Add(Facts("Member"), Caller, BoardRole.Member).Should().Be(AccessDecisionKind.Forbidden,
            "joining is a raise from no board role");
    }

    [Fact]
    public void Manager_AndWorkspaceOwner_AreUnaffected()
    {
        var manager = Facts("Member", audience: "Private", explicitRank: 4);
        Add(manager, Colleague, BoardRole.Owner).Should().Be(AccessDecisionKind.Allowed);
        Change(manager, Caller, BoardRole.Owner).Should().Be(AccessDecisionKind.Allowed);

        var owner = Facts("Owner", audience: "Private");
        Add(owner, Caller, BoardRole.Owner).Should().Be(AccessDecisionKind.Allowed);
        Change(owner, Colleague, BoardRole.Admin).Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Guest_WithAManagerGrant_IsCapped_AndCannotAssignManagerRoles()
    {
        var guest = Facts("Guest", explicitRank: 4);

        Add(guest, Colleague, BoardRole.Admin).Should().Be(AccessDecisionKind.Forbidden);
        Add(guest, Colleague, BoardRole.Member).Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void ACallerCannotChangeAMemberWhoHoldsAHigherGrant()
    {
        var editor = Facts("Member") with { TargetPermissionRank = 4 };

        Change(editor, Colleague, BoardRole.Observer).Should().Be(AccessDecisionKind.Forbidden,
            "the target's existing Manager grant raises the ceiling, as for resource permissions");
    }

    private static AccessDecisionKind Add(AccessFacts facts, Guid userId, BoardRole role)
    {
        var request = new AddBoardMemberCommand(BoardId, userId, role);
        return Engine.Evaluate(RequestDescriptorValidator.Create(typeof(AddBoardMemberCommand)), Context, facts, request).Kind;
    }

    private static AccessDecisionKind Change(AccessFacts facts, Guid userId, BoardRole role)
    {
        var request = new UpdateBoardMemberRoleCommand(BoardId, userId, role);
        return Engine.Evaluate(RequestDescriptorValidator.Create(typeof(UpdateBoardMemberRoleCommand)), Context, facts, request).Kind;
    }

    private static AccessFacts Facts(
        string role,
        string audience = "Workspace",
        string? boardRole = null,
        int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: boardRole,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null);
}
