using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// The engine waves an Anonymous or System request through without a permission check, because those
/// requests carry no permission contract. A request that is Anonymous or System AND declares a permission
/// is a misconfiguration and must be refused, never allowed.
/// </summary>
public sealed class PublicAndSystemPrincipalTests
{
    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.System, ApplicationScopeKind.Workspace, Guid.NewGuid().ToString("D"));

    private static readonly AccessFacts Facts = new(
        UserExists: true, EmailVerified: true, AccountExists: true, AccountMemberRole: null,
        WorkspaceExists: true, WorkspaceMemberRole: null, ResourceExists: true, ResourceAudience: null,
        ResourceMemberRole: null, HasExplicitResourcePermission: false, PermissionRules: [],
        SubscriptionRequirementSatisfied: false, FeatureEnabled: false,
        ActiveResourcePermissionRank: null, TargetPermissionRank: null);

    [Theory]
    [InlineData(ApplicationPrincipalKind.Anonymous)]
    [InlineData(ApplicationPrincipalKind.System)]
    public void APublicOrSystemRequestThatDeclaresAPermission_IsRefused_NotAllowed(ApplicationPrincipalKind principal)
    {
        var descriptor = new RequestDescriptor(
            typeof(object), ApplicationRequestKind.Command, principal, ApplicationScopeKind.Workspace,
            ApplicationDataAccessKind.Write, new AccessRequirements(true, false, false, false), false, false);

        var decision = new AccessPolicyEngine().Evaluate(descriptor, Context, Facts, new object());

        decision.Kind.Should().Be(AccessDecisionKind.SecurityMisconfiguration);
    }

    [Theory]
    [InlineData(ApplicationPrincipalKind.Anonymous)]
    [InlineData(ApplicationPrincipalKind.System)]
    public void APublicOrSystemRequestWithoutAPermissionContract_IsAllowed(ApplicationPrincipalKind principal)
    {
        var descriptor = new RequestDescriptor(
            typeof(object), ApplicationRequestKind.Command, principal, ApplicationScopeKind.Global,
            ApplicationDataAccessKind.Write, new AccessRequirements(false, false, false, false), false, false);

        new AccessPolicyEngine().Evaluate(descriptor, Context, Facts, new object()).Kind
            .Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EveryAnonymousOrSystemRequestOfTheApplication_IsGlobalOrTokenScoped_AndHasNoPermissionContract()
    {
        var requests = typeof(AccessPolicyEngine).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && (typeof(IAnonymousRequest).IsAssignableFrom(t) || typeof(ISystemInternalRequest).IsAssignableFrom(t)))
            .ToList();

        requests.Should().NotBeEmpty();
        foreach (var request in requests)
        {
            var descriptor = RequestDescriptorValidator.Create(request);
            descriptor.Access.RequiresPermission.Should().BeFalse(request.Name);
            descriptor.Scope.Should().BeOneOf([ApplicationScopeKind.Global, ApplicationScopeKind.Token], request.Name);
        }

        // Only these requests are reachable without a signed-in caller or a permission; a new one must be
        // added here on purpose (it is public, or it is raised by the system itself).
        requests.Select(t => t.Name).Order().Should().Equal(
            "AcceptInvitationWithRegistrationCommand", "CompleteMfaChallengeCommand", "CompleteOAuthLoginCommand",
            "CompleteOAuthStepUpCommand", "ConfirmEmailCommand", "DeclineInvitationByTokenCommand",
            "ForgotPasswordCommand", "GetInvitationByTokenQuery", "HandleCalendarWebhookCommand",
            "IdentifySignInQuery", "LoginCommand", "ProvisionPersonalWorkspaceCommand", "RefreshTokenCommand",
            "RegisterCommand", "ResendEmailVerificationCommand", "ResetPasswordCommand", "ResolveSlugQuery",
            "SendWelcomeEmailCommand", "StartOAuthLoginCommand");
    }
}
