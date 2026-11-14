using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// M2G frozen intended-policy matrix for CreateComment, evaluated against the
/// real AccessPolicyEngine with the real CreateCommentCommand descriptor.
/// Explicit allow/deny evidence per principal class — no mechanical copy of
/// the ManageBoard mapping is permitted for either comment target kind.
/// </summary>
public sealed class CreateCommentTargetPolicyMatrixTests
{
    private static readonly RequestDescriptor CommentDescriptor =
        RequestDescriptorValidator.Create(typeof(CreateCommentCommand));

    private static readonly RequestDescriptor MoveItemDescriptor =
        RequestDescriptorValidator.Create(typeof(MoveBoardItemCommand));

    [Fact]
    public void BoardItem_Owner_IsAllowed()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Owner"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void BoardItem_OrdinaryMember_IsAllowed_AsIntendedUsageRight()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Member"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void BoardItem_AdminMember_IsAllowed()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Admin"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void BoardItem_Guest_WithoutExplicitAccess_IsNotFound()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Guest"));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void BoardItem_Guest_WithExplicitResourcePermission_IsAllowed()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Guest", hasExplicitResourcePermission: true));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void BoardItem_FirstPriorityDenyRule_IsForbidden()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(
                workspaceRole: "Member",
                rules: [new AccessPermissionRule(0, "Deny")]));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void BoardItem_NonMember_IsNotFound()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: null));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void Page_OrdinaryMember_OnWorkspaceAudience_IsAllowed()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Member", resourceAudience: "Workspace"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Page_Member_OnRestrictedAudience_WithoutAccess_IsNotFound()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Member", resourceAudience: "Restricted"));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void Page_Member_OnRestrictedAudience_WithExplicitPermission_IsAllowed()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null),
            Facts(
                workspaceRole: "Member",
                resourceAudience: "Restricted",
                hasExplicitResourcePermission: true));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    /// <summary>
    /// UC-COL-C03: a read-only board role (Observer) is a Viewer and cannot comment. Item facts carry their Board,
    /// as production facts do, so the Board-child path decides the level.
    /// </summary>
    [Fact]
    public void BoardItem_ObserverBoardRole_IsForbiddenFromCommenting()
    {
        var observer = Facts(workspaceRole: "Member") with
        {
            ResourceMemberRole = "Observer",
            ParentBoardId = Guid.NewGuid(),
            IsBoardChild = true,
        };

        var decision = EvaluateComment(CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null), observer);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    /// <summary>UC-COL-C03: an explicit Viewer grant on a restricted page reads but does not comment.</summary>
    [Fact]
    public void Page_ViewerGrant_OnRestrictedAudience_IsForbiddenFromCommenting()
    {
        var viewer = Facts(workspaceRole: "Member", resourceAudience: "Restricted", hasExplicitResourcePermission: true) with
        {
            ActiveResourcePermissionRank = 1,
        };

        var decision = EvaluateComment(CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null), viewer);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    /// <summary>
    /// UC-COL-C04 (GOV-030): a Guest is capped at Viewer on a Board or Page and never comments there, even with
    /// an explicit grant. The use case's "can comment up to Editor" is an open owner question.
    /// </summary>
    [Fact]
    public void Page_Guest_WithExplicitResourcePermission_IsForbidden_GOV030()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Guest", resourceAudience: "Restricted", hasExplicitResourcePermission: true));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void Page_Guest_WithoutExplicitAccess_IsNotFound()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForPage(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: "Guest", resourceAudience: "Restricted"));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    /// <summary>
    /// The board-item usage-right branch is scoped to CreateComment only:
    /// MoveItem keeps the frozen canonical contract that denies ordinary
    /// members (pinned by M6 target-action evidence).
    /// </summary>
    [Fact]
    public void BoardItem_MoveItem_OrdinaryMember_KeepsCanonicalDeny()
    {
        var descriptor = MoveItemDescriptor;
        var snapshot = Snapshot();
        var request = new MoveBoardItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1);
        var facts = Facts(workspaceRole: "Member");

        var decision = new AccessPolicyEngine().Evaluate(descriptor, snapshot, facts, request);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    private static AccessDecision EvaluateComment(CreateCommentCommand request, AccessFacts facts) =>
        new AccessPolicyEngine().Evaluate(CommentDescriptor, Snapshot(), facts, request);

    private static ExecutionContextSnapshot Snapshot() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource,
        Guid.NewGuid().ToString("D"));

    private static AccessFacts Facts(
        string? workspaceRole,
        string? resourceAudience = null,
        bool hasExplicitResourcePermission = false,
        AccessPermissionRule[]? rules = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Owner",
        WorkspaceExists: true,
        WorkspaceMemberRole: workspaceRole,
        ResourceExists: true,
        ResourceAudience: resourceAudience,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: hasExplicitResourcePermission,
        PermissionRules: rules ?? [],
        SubscriptionRequirementSatisfied: false,
        FeatureEnabled: false,
        ActiveResourcePermissionRank: hasExplicitResourcePermission ? 2 : null,
        TargetPermissionRank: null);
}
