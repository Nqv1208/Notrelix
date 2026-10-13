using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Acting on another member is bounded like assigning a role: a caller below
/// Manager cannot remove (or demote) a board member whose level — the higher of
/// their board role and any explicit grant — exceeds the caller's own level on
/// the Board. Managers and workspace Owners are unaffected; members at or below
/// the caller's level can still be removed, and a caller can leave the Board.
/// </summary>
public sealed class BoardMemberRemovalCeilingTests
{
    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid Caller = Guid.Parse("00000000-0000-0000-0000-00000000ca11");
    private static readonly Guid Target = Guid.Parse("00000000-0000-0000-0000-0000000000c0");
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");

    private static readonly ExecutionContextSnapshot Context = new(
        Caller, Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    public static TheoryData<string, string?, int?, AccessDecisionKind> Removals() => new()
    {
        // caller source, target board role, target explicit grant rank, expected
        { "editor", "Owner", null, AccessDecisionKind.Forbidden },
        { "editor", "Admin", null, AccessDecisionKind.Forbidden },
        { "editor", "Member", 4, AccessDecisionKind.Forbidden },
        { "editor", "Member", null, AccessDecisionKind.Allowed },
        { "editor", "Observer", null, AccessDecisionKind.Allowed },
        { "editor", "Guest", null, AccessDecisionKind.Allowed },
        { "editor", null, null, AccessDecisionKind.Allowed },
        { "guest with a Manager grant", "Owner", null, AccessDecisionKind.Forbidden },
        { "manager", "Owner", null, AccessDecisionKind.Allowed },
        { "manager", "Admin", 4, AccessDecisionKind.Allowed },
        { "workspace owner", "Owner", 5, AccessDecisionKind.Allowed },
    };

    [Theory]
    [MemberData(nameof(Removals))]
    public void Removal_IsBoundedByTheCallersLevel(string caller, string? targetRole, int? targetGrant, AccessDecisionKind expected)
    {
        var facts = CallerFacts(caller) with { TargetBoardMemberRole = targetRole, TargetPermissionRank = targetGrant };

        Remove(facts, Target).Should().Be(expected, $"{caller} removing a {targetRole ?? "non-member"} (grant {targetGrant?.ToString() ?? "-"})");
    }

    [Fact]
    public void AnEditor_CanLeaveTheBoard()
    {
        var facts = CallerFacts("editor") with { ResourceMemberRole = "Member", TargetBoardMemberRole = "Member" };

        Remove(facts, Caller).Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void AnEditor_CannotDemoteABoardOwner()
    {
        var facts = CallerFacts("editor") with { TargetBoardMemberRole = "Owner" };
        var request = new UpdateBoardMemberRoleCommand(BoardId, Target, BoardRole.Observer);

        Engine.Evaluate(RequestDescriptorValidator.Create(typeof(UpdateBoardMemberRoleCommand)), Context, facts, request).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "changing a Manager-level member is above the caller's level");
    }

    private static AccessDecisionKind Remove(AccessFacts facts, Guid userId)
    {
        var request = new RemoveBoardMemberCommand(BoardId, userId);
        return Engine.Evaluate(RequestDescriptorValidator.Create(typeof(RemoveBoardMemberCommand)), Context, facts, request).Kind;
    }

    private static AccessFacts CallerFacts(string caller) => caller switch
    {
        "editor" => Facts("Member"),
        "guest with a Manager grant" => Facts("Guest", explicitRank: 4),
        "manager" => Facts("Member", audience: "Private", explicitRank: 4),
        "workspace owner" => Facts("Owner", audience: "Private"),
        _ => throw new ArgumentOutOfRangeException(nameof(caller), caller, null),
    };

    private static AccessFacts Facts(string role, string audience = "Workspace", int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null);
}
