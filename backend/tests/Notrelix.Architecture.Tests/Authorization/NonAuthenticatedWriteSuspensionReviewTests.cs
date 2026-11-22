using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Architecture.Tests.Authorization;

/// <summary>
/// A suspended Account is read-only. Authenticated tenant writes are stopped by the access policy
/// (<see cref="AccountSuspensionWriteGateArchitectureTests"/>); the writes below carry no member session, or enter
/// by a token, so each one is reviewed by name: what it does on a suspended Account and why. A new anonymous,
/// system or token-scoped write fails this test until someone decides, in writing, how it treats suspension.
/// </summary>
public sealed class NonAuthenticatedWriteSuspensionReviewTests
{
    private const string Personal = "Personal identity or credential write; it holds no Account content, so suspension does not apply.";

    /// <summary>Short request name to the reviewed treatment of a suspended Account.</summary>
    private static readonly Dictionary<string, string> Reviewed = new()
    {
        ["SubmitPublicFormCommand"] =
            "Refused: PublicFormScope reads IAccountStatusFacts (PublicFormAppRoleTests, AccountSuspensionAppRoleTests).",
        ["AcceptInvitationCommand"] =
            "Refused: the signed-in acceptance (this one and AcceptInvitationById) shares InvitationAcceptanceService, which admits a member only while the Account admits members (AccountMembershipFactsProviderTests).",
        ["AcceptInvitationWithRegistrationCommand"] =
            "Refused: InvitationAcceptanceService admits a member only while the Account admits members (AccountMembershipFactsProviderTests).",
        ["DeclineInvitationByTokenCommand"] =
            "Allowed: it changes only the invitee's own invitation row and adds nothing to the Account.",
        ["HandleCalendarWebhookCommand"] =
            "Allowed: it writes the Integrations receipt and the calendar link bookkeeping (sync hash, ETag), never a work item or a page.",
        ["HandleCallMediaWebhookCommand"] =
            "Allowed: the media provider reports a call ending or a participant leaving; it reconciles the call's own state and creates no member content.",
        ["SendWelcomeEmailCommand"] = "Allowed: sends mail and writes no Account content.",
        ["ProvisionPersonalWorkspaceCommand"] =
            "Allowed: creates the registrant's own first workspace in the personal Account the same registration just created.",
        ["LoginCommand"] = Personal,
        ["RefreshTokenCommand"] = Personal,
        ["ResetPasswordCommand"] = Personal,
        ["CompleteMfaChallengeCommand"] = Personal,
        ["CompleteRequiredMfaEnrollmentCommand"] = Personal,
        ["StartRequiredMfaEnrollmentCommand"] = Personal,
        ["CompleteOAuthLoginCommand"] = Personal,
        ["CompleteOAuthStepUpCommand"] = Personal,
        ["RegisterCommand"] = Personal,
        ["CompleteSsoSignInCommand"] = Personal,
        ["ResendEmailVerificationCommand"] = Personal,
        ["ConfirmEmailCommand"] = Personal,
    };

    [Fact]
    public void EveryNonAuthenticatedOrTokenScopedWrite_IsReviewedForSuspension()
    {
        var registry = RequestDescriptorRegistry.Create(typeof(ICommand<>).Assembly);

        var actual = registry.Descriptors
            .Where(d => d.DataAccess == ApplicationDataAccessKind.Write
                && (d.Principal != ApplicationPrincipalKind.Authenticated || d.Scope == ApplicationScopeKind.Token))
            .Select(d => d.RequestType.Name)
            .ToList();

        actual.Count.Should().BeGreaterThan(10, "an empty scan proves nothing");
        actual.Except(Reviewed.Keys).Should().BeEmpty(
            "these writes have no reviewed treatment of a suspended Account; decide it and add them to the list above");
        Reviewed.Keys.Except(actual).Should().BeEmpty(
            "these reviewed requests are gone or are no longer non-authenticated writes; remove them from the list");
        Reviewed.Values.Should().OnlyContain(reason => reason.Length > 20);
    }
}
