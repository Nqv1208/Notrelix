namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

public enum MfaChallengePurpose
{
    PasswordLogin,
    OAuthLogin,
    StepUpDisableMfa,
    StepUpRegenerateRecoveryCodes,
    StepUpLinkOAuth,
    StepUpUnlinkOAuth,
    StepUpIssueApiToken,
    StepUpChangeSecurityIdentity
}

/// <summary>
/// Durable payload behind a single-use MFA challenge token.
/// Stored transiently (Redis); consumed exactly once on verification.
/// </summary>
public sealed record MfaChallengePayload(
    Guid UserId,
    MfaChallengePurpose Purpose,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    Guid? SessionId = null);
