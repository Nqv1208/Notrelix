namespace Notrelix.Application.Features.Identity.Security.DTOs;

/// <summary>
/// Security operation protected by strong step-up verification.
/// Each purpose binds the issued proof to the intended operation.
/// </summary>
public enum StepUpPurpose
{
    DisableMfa,
    RegenerateRecoveryCodes,
    LinkOAuth,
    UnlinkOAuth,
    IssueApiToken,
    ChangeSecurityIdentity
}

/// <summary>
/// Factor the user must satisfy for step-up verification, derived from
/// the user's enrolled factors (MFA first, then password credential, then OAuth re-authentication).
/// </summary>
public enum StepUpRequiredFactor
{
    MfaChallenge,
    Password,
    OAuth
}