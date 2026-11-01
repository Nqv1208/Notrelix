using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Removing members is Board management, which needs Manager: below Manager no
/// one removes a member (including leaving the Board, previously allowed at or
/// below the caller's level). A Manager removes members up to the ceiling of
/// their own level (the target's level is the higher of their board role and
/// any explicit grant); workspace Owners remove anyone.
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
        // Below Manager every removal is refused (previously members at or below
        // the Editor's level could be removed).
        { "editor", "Owner", null, AccessDecisionKind.Forbidden },
        { "editor", "Admin", null, AccessDecisionKind.Forbidden },
        { "editor", "Member", 4, AccessDecisionKind.Forbidden },
        { "editor", "Member", null, AccessDecisionKind.Forbidden },
        { "editor", "Observer", null, AccessDecisionKind.Forbidden },
        { "editor", "Guest", null, AccessDecisionKind.Forbidden },
        { "editor", null, null, AccessDecisionKind.Forbidden },
        { "guest with a Manager grant", "Owner", null, AccessDecisionKind.Forbidden },
        { "guest with a Manager grant", "Observer", null, AccessDecisionKind.Forbidden },
        { "manager", "Owner", null, AccessDecisionKind.Allowed },
        { "manager", "Admin", 4, AccessDecisionKind.Allowed },
        { "manager", "Member", null, AccessDecisionKind.Allowed },
        { "manager", "Member", 5, AccessDecisionKind.Forbidden },
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
    public void AnEditor_CannotRemoveThemselvesEither()
    {
        // Leaving is removing a member: Board management, Manager only.
        var facts = CallerFacts("editor") with { ResourceMemberRole = "Member", TargetBoardMemberRole = "Member" };

        Remove(facts, Caller).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void AnEditor_CannotDemoteABoardOwner()
    {
        var facts = CallerFacts("editor") with { TargetBoardMemberRole = "Owner" };
        var request = new UpdateBoardMemberRoleCommand(BoardId, Target, BoardRole.Observer, ExpectedVersion: 1);

        Engine.Evaluate(RequestDescriptorValidator.Create(typeof(UpdateBoardMemberRoleCommand)), Context, facts, request).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "changing a Manager-level member is above the caller's level");
    }

    private static AccessDecisionKind Remove(AccessFacts facts, Guid userId)
    {
        var request = new RemoveBoardMemberCommand(BoardId, userId, ExpectedVersion: 1);
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
