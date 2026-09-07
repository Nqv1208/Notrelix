using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>
/// M2G frozen intended-policy matrix for CreatePage, evaluated against the
/// real AccessPolicyEngine with the real CreatePageCommand descriptor:
/// Action=CreatePage on the workspace resource. Page creation is a workspace
/// usage right for authenticated members; guests are excluded; explicit
/// deny-first permission rules still apply.
/// </summary>
public sealed class CreatePageTargetPolicyMatrixTests
{
    private static readonly RequestDescriptor CreatePageDescriptor =
        RequestDescriptorValidator.Create(typeof(CreatePageCommand));

    [Fact]
    public void Owner_IsAllowed()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Owner Page", null),
            Facts(workspaceRole: "Owner"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Admin_IsAllowed()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Admin Page", null),
            Facts(workspaceRole: "Admin"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void OrdinaryMember_IsAllowed_AsIntendedUsageRight()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Member Page", null),
            Facts(workspaceRole: "Member"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Guest_IsForbidden()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Guest Page", null),
            Facts(workspaceRole: "Guest"));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void NonMember_IsForbidden()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Outsider Page", null),
            Facts(workspaceRole: null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void FirstPriorityDenyRule_IsForbidden()
    {
        var decision = Evaluate(
            new CreatePageCommand(Guid.NewGuid(), "Denied Page", null),
            Facts(
                workspaceRole: "Member",
                rules: [new AccessPermissionRule(0, "Deny")]));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    private static AccessDecision Evaluate(CreatePageCommand request, AccessFacts facts) =>
        new AccessPolicyEngine().Evaluate(CreatePageDescriptor, Snapshot(), facts, request);

    private static ExecutionContextSnapshot Snapshot() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Workspace,
        Guid.NewGuid().ToString("D"));

    private static AccessFacts Facts(
        string? workspaceRole,
        AccessPermissionRule[]? rules = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Owner",
        WorkspaceExists: true,
        WorkspaceMemberRole: workspaceRole,
        ResourceExists: true,
        ResourceAudience: null,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: false,
        PermissionRules: rules ?? [],
        HasActiveSubscription: false,
        SubscriptionTier: null,
        FeatureEnabled: false,
        ActiveResourcePermissionRank: null,
        TargetPermissionRank: null);
}
