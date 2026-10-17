using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Every source that gives access to a Board or Page grants a level, and the
/// engine enforces the action threshold of that level (GOV-027 table: Viewer
/// reads, Commenter also comments, Editor does every other Board/Page action,
/// Manager also manages permissions and archives Pages) whatever the source:
/// explicit User/Team grant, board membership, Space role, Workspace default.
/// A Workspace Guest is capped at Editor whatever the source. The reported
/// effective level equals what the engine enforces.
/// </summary>
public sealed class ResourceLevelEnforcementMatrixTests
{
    private const string Board = "work-management.board";
    private const string Page = "documents.page";

    private static readonly AccessPolicyEngine Engine = new();

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor Descriptor = new(
        typeof(Probe), ApplicationRequestKind.Query, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Read,
        new AccessRequirements(true, false, false, false), false, false);

    /// <summary>Representative Board actions and the level each requires.</summary>
    private static readonly (PermissionAction Action, int Required)[] BoardActions =
    [
        (PermissionAction.ViewBoard, 1),
        (PermissionAction.CreateComment, 2),
        (PermissionAction.UpdateItem, 3),
        (PermissionAction.CreateItem, 3),
        (PermissionAction.CreateField, 3),
        // Managing the Board needs Manager (Editors only edit content).
        (PermissionAction.ManageBoard, 4),
        (PermissionAction.ManageBoardPermission, 4),
    ];

    private static readonly (PermissionAction Action, int Required)[] PageActions =
    [
        (PermissionAction.ViewPage, 1),
        (PermissionAction.CreateComment, 2),
        (PermissionAction.UpdatePage, 3),
        (PermissionAction.ManageBoard, 3),
        (PermissionAction.ManagePagePermission, 4),
        (PermissionAction.ArchivePage, 4),
    ];

    public static TheoryData<string, string, int> BoardSources() => new()
    {
        // source, workspace role, expected level (1 Viewer .. 4 Manager)
        { "explicit Viewer on a Private board", "Member", 1 },
        { "explicit Commenter on a Private board", "Member", 2 },
        { "explicit Editor on a Private board", "Member", 3 },
        { "explicit Manager on a Private board", "Member", 4 },
        { "board Observer", "Member", 1 },
        { "board Guest role", "Member", 1 },
        { "board Member", "Member", 3 },
        { "board Admin", "Member", 4 },
        { "board Owner", "Member", 4 },
        { "Space Viewer", "Member", 1 },
        { "Space Member", "Member", 3 },
        { "Space Admin", "Member", 4 },
        { "open Space default", "Member", 3 },
        { "workspace Admin in a Space", "Admin", 4 },
        { "workspace default, unplaced", "Member", 3 },
        { "workspace Admin, unplaced", "Admin", 3 },
        { "explicit Viewer in a Space where the caller is Space Member", "Member", 3 },
        { "workspace Owner", "Owner", 5 },
        { "guest with explicit Manager", "Guest", 3 },
        { "guest board Admin", "Guest", 3 },
        { "guest Space Admin", "Guest", 3 },
        { "guest with explicit Commenter", "Guest", 2 },
    };

    [Theory]
    [MemberData(nameof(BoardSources))]
    public void BoardActions_AreAllowedExactlyFromTheSourceLevel(string source, string role, int level)
    {
        var facts = BoardFacts(source, role);

        foreach (var (action, required) in BoardActions)
        {
            var decision = Engine.Evaluate(Descriptor, Context, facts, new Probe(action, Board));
            decision.Kind.Should().Be(level >= required ? AccessDecisionKind.Allowed : AccessDecisionKind.Forbidden,
                $"{source} ({LevelName(level)}) → {action}");
        }

        AccessPolicyEngine.EvaluateEffectiveBoardAccess(facts).Level.Should().Be(LevelName(level),
            $"the reported level of {source} must be the enforced level");
    }

    public static TheoryData<string, string, int> PageSources() => new()
    {
        { "explicit Viewer on a Private page", "Member", 1 },
        { "explicit Commenter on a Private page", "Member", 2 },
        { "explicit Editor on a Private page", "Member", 3 },
        { "explicit Manager on a Private page", "Member", 4 },
        { "Space Viewer", "Member", 1 },
        { "Space Member", "Member", 3 },
        { "workspace default, unplaced", "Member", 3 },
        { "guest with explicit Manager", "Guest", 3 },
    };

    [Theory]
    [MemberData(nameof(PageSources))]
    public void PageActions_AreAllowedExactlyFromTheSourceLevel(string source, string role, int level)
    {
        var facts = PageFacts(source, role);

        foreach (var (action, required) in PageActions)
        {
            var decision = Engine.Evaluate(Descriptor, Context, facts, new Probe(action, Page));
            decision.Kind.Should().Be(level >= required ? AccessDecisionKind.Allowed : AccessDecisionKind.Forbidden,
                $"{source} ({LevelName(level)}) → {action}");
        }
    }

    [Fact]
    public void Guest_WithAManagerGrant_CannotGrantOrManagePermissions()
    {
        var facts = BoardFacts("guest with explicit Manager", "Guest");

        Engine.Evaluate(Descriptor, Context, facts, new GrantProbe(Board, RequestedRank: 3)).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "granting is a ManageBoardPermission action, denied to a Guest (GOV-030)");
    }

    [Fact]
    public void VisibilityIsUnchanged_NoAccessIsStillNotFound()
    {
        Engine.Evaluate(Descriptor, Context, Facts("Member", audience: "Private"), new Probe(PermissionAction.ViewBoard, Board)).Kind
            .Should().Be(AccessDecisionKind.NotFound);
        Engine.Evaluate(Descriptor, Context, Facts("Guest"), new Probe(PermissionAction.UpdateItem, Board)).Kind
            .Should().Be(AccessDecisionKind.NotFound, "a Guest without explicit access cannot see an unplaced board");
        Engine.Evaluate(Descriptor, Context, Facts("Member", spaceId: Guid.NewGuid()), new Probe(PermissionAction.ManageBoard, Board)).Kind
            .Should().Be(AccessDecisionKind.NotFound, "no Space access, no entry");
    }

    private static AccessFacts BoardFacts(string source, string role) => source switch
    {
        "explicit Viewer on a Private board" => Facts(role, audience: "Private", explicitRank: 1),
        "explicit Commenter on a Private board" => Facts(role, audience: "Private", explicitRank: 2),
        "explicit Editor on a Private board" => Facts(role, audience: "Private", explicitRank: 3),
        "explicit Manager on a Private board" => Facts(role, audience: "Private", explicitRank: 4),
        "board Observer" => Facts(role, audience: "Private", boardRole: "Observer"),
        "board Guest role" => Facts(role, audience: "Private", boardRole: "Guest"),
        "board Member" => Facts(role, audience: "Private", boardRole: "Member"),
        "board Admin" => Facts(role, audience: "Private", boardRole: "Admin"),
        "board Owner" => Facts(role, audience: "Private", boardRole: "Owner"),
        "Space Viewer" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Viewer"),
        "Space Member" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Member"),
        "Space Admin" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Admin"),
        "open Space default" => Facts(role, spaceId: Guid.NewGuid(), spaceIsOpen: true),
        "workspace Admin in a Space" => Facts(role, spaceId: Guid.NewGuid()),
        "workspace default, unplaced" => Facts(role),
        "workspace Admin, unplaced" => Facts(role),
        "explicit Viewer in a Space where the caller is Space Member" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Member", explicitRank: 1),
        "workspace Owner" => Facts(role, audience: "Private"),
        "guest with explicit Manager" => Facts(role, explicitRank: 4),
        "guest board Admin" => Facts(role, audience: "Private", boardRole: "Admin"),
        "guest Space Admin" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Admin"),
        "guest with explicit Commenter" => Facts(role, explicitRank: 2),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    private static AccessFacts PageFacts(string source, string role) => source switch
    {
        "explicit Viewer on a Private page" => Facts(role, audience: "Private", explicitRank: 1),
        "explicit Commenter on a Private page" => Facts(role, audience: "Private", explicitRank: 2),
        "explicit Editor on a Private page" => Facts(role, audience: "Private", explicitRank: 3),
        "explicit Manager on a Private page" => Facts(role, audience: "Private", explicitRank: 4),
        "Space Viewer" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Viewer"),
        "Space Member" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Member"),
        "workspace default, unplaced" => Facts(role),
        "guest with explicit Manager" => Facts(role, explicitRank: 4),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    private static string LevelName(int level) => level switch
    {
        >= 4 => "Manager",
        3 => "Editor",
        2 => "Commenter",
        _ => "Viewer",
    };

    private static AccessFacts Facts(
        string role,
        string audience = "Workspace",
        Guid? spaceId = null,
        string? spaceRole = null,
        bool spaceIsOpen = false,
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
        TargetPermissionRank: null)
        {
            ResourceSpaceId = spaceId,
            SpaceExists = spaceId is not null,
            SpaceRole = spaceRole,
            SpaceIsOpen = spaceIsOpen,
        };

    private sealed record Probe(PermissionAction Action, string Kind) : IRequirePermission
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000b1"));
    }

    private sealed record GrantProbe(string Kind, int RequestedRank) : IRequirePermission, IRequireGrantPermission
    {
        public PermissionAction Action => PermissionAction.ManageBoardPermission;
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000b1"));
        public int RequestedPermissionRank => RequestedRank;
    }
}
