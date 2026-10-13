using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Board roles carry levels (Owner/Admin → Manager), so assigning a role is a
/// grant bounded by the caller's own level. Managing members is Board
/// management, which needs Manager: below Manager no one adds members or
/// changes their roles (including their own). Managers and workspace Owners
/// keep full rights, up to the ceiling of their own level.
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
    public void Editor_CannotManageMembers_EvenAtOrBelowTheirLevel(BoardRole role)
    {
        // Previously an Editor could assign roles at or below Editor. Managing
        // members is Board management, which now needs Manager.
        var editor = Facts("Member");

        Add(editor, Colleague, role).Should().Be(AccessDecisionKind.Forbidden);
        Change(editor, Colleague, role).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void Observer_CannotChangeAnyRole()
    {
        // Workspace default gives Editor, still below the Manager needed to
        // manage members; previously the non-raising changes were allowed.
        var observer = Facts("Member", boardRole: "Observer");

        Change(observer, Caller, BoardRole.Member).Should().Be(AccessDecisionKind.Forbidden);
        Change(observer, Caller, BoardRole.Observer).Should().Be(AccessDecisionKind.Forbidden);
        Change(observer, Colleague, BoardRole.Member).Should().Be(AccessDecisionKind.Forbidden);
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
    public void Guest_WithAManagerGrant_IsCapped_AndCannotManageMembers()
    {
        // A Guest is capped at Editor, below the Manager needed to manage
        // members (previously a Guest could add members up to Editor).
        var guest = Facts("Guest", explicitRank: 4);

        Add(guest, Colleague, BoardRole.Admin).Should().Be(AccessDecisionKind.Forbidden);
        Add(guest, Colleague, BoardRole.Member).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void ACallerCannotChangeAMemberWhoHoldsAHigherGrant()
    {
        var manager = Facts("Member", audience: "Private", explicitRank: 4) with { TargetPermissionRank = 5 };

        Change(manager, Colleague, BoardRole.Observer).Should().Be(AccessDecisionKind.Forbidden,
            "the target's existing Owner-level grant raises the ceiling above the Manager's level");
        Change(Facts("Member") with { TargetPermissionRank = 4 }, Colleague, BoardRole.Observer)
            .Should().Be(AccessDecisionKind.Forbidden);
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
