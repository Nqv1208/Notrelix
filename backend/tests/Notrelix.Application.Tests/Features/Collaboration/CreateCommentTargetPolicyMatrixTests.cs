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
    public void BoardItem_NonMember_IsForbidden()
    {
        var decision = EvaluateComment(
            CreateCommentCommand.ForBoardItem(Guid.NewGuid(), "Hello", null),
            Facts(workspaceRole: null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
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
    /// The board-item usage-right branch is scoped to CreateComment only:
    /// MoveItem keeps the frozen canonical contract that denies ordinary
    /// members (pinned by M6 target-action evidence).
    /// </summary>
    [Fact]
    public void BoardItem_MoveItem_OrdinaryMember_KeepsCanonicalDeny()
    {
        var descriptor = MoveItemDescriptor;
        var snapshot = Snapshot();
        var request = new MoveBoardItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1.0d);
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
        HasActiveSubscription: false,
        SubscriptionTier: null,
        FeatureEnabled: false,
        ActiveResourcePermissionRank: hasExplicitResourcePermission ? 2 : null,
        TargetPermissionRank: null);
}
