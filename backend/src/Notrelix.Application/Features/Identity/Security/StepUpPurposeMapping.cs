using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.DTOs;

namespace Notrelix.Application.Features.Identity.Security;

internal static class StepUpPurposeMapping
{
    public static MfaChallengePurpose ToChallengePurpose(StepUpPurpose purpose) => purpose switch
    {
        StepUpPurpose.DisableMfa => MfaChallengePurpose.StepUpDisableMfa,
        StepUpPurpose.RegenerateRecoveryCodes => MfaChallengePurpose.StepUpRegenerateRecoveryCodes,
        StepUpPurpose.LinkOAuth => MfaChallengePurpose.StepUpLinkOAuth,
        StepUpPurpose.UnlinkOAuth => MfaChallengePurpose.StepUpUnlinkOAuth,
        StepUpPurpose.IssueApiToken => MfaChallengePurpose.StepUpIssueApiToken,
        StepUpPurpose.ChangeSecurityIdentity => MfaChallengePurpose.StepUpChangeSecurityIdentity,
        StepUpPurpose.ChangePassword => MfaChallengePurpose.StepUpChangePassword,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown step-up purpose")
    };

    public static StepUpPurpose FromChallengePurpose(MfaChallengePurpose purpose) => purpose switch
    {
        MfaChallengePurpose.StepUpDisableMfa => StepUpPurpose.DisableMfa,
        MfaChallengePurpose.StepUpRegenerateRecoveryCodes => StepUpPurpose.RegenerateRecoveryCodes,
        MfaChallengePurpose.StepUpLinkOAuth => StepUpPurpose.LinkOAuth,
        MfaChallengePurpose.StepUpUnlinkOAuth => StepUpPurpose.UnlinkOAuth,
        MfaChallengePurpose.StepUpIssueApiToken => StepUpPurpose.IssueApiToken,
        MfaChallengePurpose.StepUpChangeSecurityIdentity => StepUpPurpose.ChangeSecurityIdentity,
        MfaChallengePurpose.StepUpChangePassword => StepUpPurpose.ChangePassword,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Not a step-up purpose")
    };
}