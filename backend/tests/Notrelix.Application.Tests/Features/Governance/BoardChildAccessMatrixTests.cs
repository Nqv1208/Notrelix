using Notrelix.Application.Features.WorkManagement.Public.Authorization;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// A Board child resource is decided on its parent Board: the facts carry the
/// Board's audience, membership, grants and Space, plus
/// <see cref="AccessFacts.ParentBoardId"/>, and the engine applies the Board's
/// visibility (NotFound), level thresholds and Guest cap to the child's action.
/// Without a parent Board the child keeps the resource-kind fallthrough.
/// </summary>
public sealed class BoardChildAccessMatrixTests
{
    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor Descriptor = new(
        typeof(Probe), ApplicationRequestKind.Query, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Read,
        new AccessRequirements(true, false, false, false), false, false);

    /// <summary>Child kind, its mutate action (Editor), and whether it takes comments.</summary>
    private static readonly (string Kind, PermissionAction Mutate, bool Comments)[] Kinds =
    [
        ("work-management.board-item", PermissionAction.UpdateItem, true),
        ("work-management.board-item", PermissionAction.MoveItem, true),
        ("work-management.board-item", PermissionAction.AssignItem, true),
        ("work-management.board-field", PermissionAction.UpdateField, false),
        ("work-management.board-field", PermissionAction.DeleteField, false),
        // Group and label commands declare the content action they perform
        // (ManageBoard on a Board is Manager-only Board management).
        ("work-management.board-group", PermissionAction.UpdateItem, false),
        ("work-management.board-view", PermissionAction.UpdateBoardView, false),
        ("work-management.saved-filter", PermissionAction.UpdateBoardView, false),
        ("work-management.form", PermissionAction.CreateItem, false),
        ("work-management.form-submission", PermissionAction.CreateItem, false),
        ("work-management.label", PermissionAction.UpdateField, false),
        ("work-management.checklist", PermissionAction.UpdateItem, false),
        ("work-management.checklist-item", PermissionAction.UpdateItem, false),
    ];

    public static TheoryData<string, string, int> Sources() => new()
    {
        // source, workspace role, level (0 = no access → NotFound)
        { "member without board access (Private board)", "Member", 0 },
        { "guest without board access (unplaced board)", "Guest", 0 },
        { "explicit Viewer", "Member", 1 },
        { "explicit Commenter", "Member", 2 },
        { "explicit Editor", "Member", 3 },
        { "board Observer", "Member", 1 },
        { "board Member", "Member", 3 },
        { "Space Member", "Member", 3 },
        { "workspace default, unplaced", "Member", 3 },
        { "guest with explicit Manager", "Guest", 1 },
        { "workspace Owner", "Owner", 5 },
    };

    [Theory]
    [MemberData(nameof(Sources))]
    public void ChildActions_FollowTheParentBoardDecision(string source, string role, int level)
    {
        var facts = SourceFacts(source, role);

        foreach (var (kind, mutate, comments) in Kinds)
        {
            Expect(facts, kind, PermissionAction.ViewBoard, level, required: 1, source);
            Expect(facts, kind, mutate, level, required: 3, source);
            if (comments)
                Expect(facts, kind, PermissionAction.CreateComment, level, required: 2, source);
        }
    }

    [Fact]
    public void ObserverWithAnEditorGrant_StillCannotUpdateItems()
    {
        var facts = Facts("Member", audience: "Private", boardRole: "Observer", explicitRank: 3);

        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.UpdateItem, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "board-member semantics still apply (GOV-027)");
    }

    [Fact]
    public void ChildOfAMissingBoard_IsNotFound_EvenForTheOwner()
    {
        var facts = Facts("Owner", exists: false);

        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.NotFound);
    }

    [Theory]
    [InlineData("Owner", "Private")]
    [InlineData("Member", "Workspace")]
    public void ChildOfAnArchivedBoard_StaysReadable_AndRefusesEveryWrite_OnceTheNormalDecisionAllows(string role, string audience)
    {
        var facts = Facts(role, audience: audience, archived: true);

        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.Allowed, "an archived Board is read like any other (GOV-025 step 1)");

        foreach (var kind in Kinds.Select(k => k.Kind).Distinct())
        {
            var refused = Engine.Evaluate(Descriptor, Context, facts, new WriteProbe(PermissionAction.UpdateItem, kind));
            refused.Kind.Should().Be(AccessDecisionKind.Forbidden, $"a write on {kind} of an archived Board");
            refused.Code.Should().Be(WorkManagementDenialCodes.BoardArchived);
        }
    }

    [Fact]
    public void ArchivedBoard_IsNeverNamedToAPersonWhoseNormalDecisionIsNotAllow()
    {
        var noAccess = Facts("Member", audience: "Private", archived: true);
        var viewer = Facts("Member", audience: "Private", explicitRank: 1, archived: true);

        Engine.Evaluate(Descriptor, Context, noAccess, new WriteProbe(PermissionAction.UpdateItem, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.NotFound, "a caller without access gets the identical NotFound, never \"archived\"");

        var viewerWrite = Engine.Evaluate(Descriptor, Context, viewer, new WriteProbe(PermissionAction.UpdateItem, "work-management.board-item"));
        viewerWrite.Kind.Should().Be(AccessDecisionKind.Forbidden);
        viewerWrite.Code.Should().BeNull("the level refusal comes first, so it carries no archived code");
    }

    [Fact]
    public void BoardLifecycleCommands_PassOnAnArchivedBoard_AndEveryOtherBoardWriteIsRefused()
    {
        var facts = Facts("Owner", archived: true) with { ParentBoardId = null, IsBoardChild = false };

        Engine.Evaluate(Descriptor, Context, facts, new LifecycleProbe(PermissionAction.ManageBoard)).Kind
            .Should().Be(AccessDecisionKind.Allowed, "unarchive, delete and restore are the way back");

        var rename = Engine.Evaluate(Descriptor, Context, facts, new WriteProbe(PermissionAction.ManageBoard, "work-management.board"));
        rename.Kind.Should().Be(AccessDecisionKind.Forbidden);
        rename.Code.Should().Be(WorkManagementDenialCodes.BoardArchived);
    }

    [Fact]
    public void ItemInAnArchivedGroup_StaysReadable_AndRefusesWrites_WithTheGroupCode()
    {
        var facts = Facts("Member") with { ChildInArchivedGroup = true };

        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.Allowed);

        var refused = Engine.Evaluate(Descriptor, Context, facts, new WriteProbe(PermissionAction.UpdateItem, "work-management.board-item"));
        refused.Kind.Should().Be(AccessDecisionKind.Forbidden);
        refused.Code.Should().Be(WorkManagementDenialCodes.GroupArchived);
    }

    [Fact]
    public void WithoutAParentBoard_TheChildKeepsTheResourceKindFallthrough()
    {
        var facts = Facts("Member", audience: "Private") with { ParentBoardId = null };

        Engine.Evaluate(Descriptor, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.approval-request")).Kind
            .Should().Be(AccessDecisionKind.Allowed, "an approval request on a Page is not a Board child");
    }

    private static void Expect(AccessFacts facts, string kind, PermissionAction action, int level, int required, string source)
    {
        var expected = level == 0
            ? AccessDecisionKind.NotFound
            : level >= required ? AccessDecisionKind.Allowed : AccessDecisionKind.Forbidden;

        Engine.Evaluate(Descriptor, Context, facts, new Probe(action, kind)).Kind
            .Should().Be(expected, $"{source} → {action} on {kind}");
    }

    private static AccessFacts SourceFacts(string source, string role) => source switch
    {
        "member without board access (Private board)" => Facts(role, audience: "Private"),
        "guest without board access (unplaced board)" => Facts(role),
        "explicit Viewer" => Facts(role, audience: "Private", explicitRank: 1),
        "explicit Commenter" => Facts(role, audience: "Private", explicitRank: 2),
        "explicit Editor" => Facts(role, audience: "Private", explicitRank: 3),
        "board Observer" => Facts(role, audience: "Private", boardRole: "Observer"),
        "board Member" => Facts(role, audience: "Private", boardRole: "Member"),
        "Space Member" => Facts(role, spaceId: Guid.NewGuid(), spaceRole: "Member"),
        "workspace default, unplaced" => Facts(role),
        "guest with explicit Manager" => Facts(role, explicitRank: 4),
        "workspace Owner" => Facts(role, audience: "Private"),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    private static AccessFacts Facts(
        string role,
        string audience = "Workspace",
        Guid? spaceId = null,
        string? spaceRole = null,
        string? boardRole = null,
        int? explicitRank = null,
        bool exists = true,
        bool archived = false) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: exists,
        ResourceAudience: exists ? audience : null,
        ResourceMemberRole: boardRole,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null)
        {
            ParentBoardId = BoardId,
            ResourceSpaceId = spaceId,
            SpaceExists = spaceId is not null,
            SpaceRole = spaceRole,
            BoardArchived = archived,
        };

    private sealed record Probe(PermissionAction Action, string Kind) : IRequirePermission
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000c1"));
    }

    private sealed record WriteProbe(PermissionAction Action, string Kind) : IRequirePermission, IWriteRequest
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000c1"));
    }

    private sealed record LifecycleProbe(PermissionAction Action) : IRequirePermission, IWriteRequest, IBoardLifecycleRequest
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
    }
}
