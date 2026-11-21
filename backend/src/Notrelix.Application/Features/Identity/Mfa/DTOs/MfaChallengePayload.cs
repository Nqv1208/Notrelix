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
    StepUpChangeSecurityIdentity,
    StepUpChangePassword,
    StepUpRevokeOtherSessions,
    StepUpEnrollMfa,
    StepUpTransferWorkspaceOwnership,
    StepUpResetMemberMfa,

    /// <summary>
    /// Sign-in stopped because the person's Account requires MFA and they hold no factor. The token only
    /// authorizes enrolling one (the two required-enrollment endpoints), then finishes the sign-in.
    /// </summary>
    RequiredEnrollment
}

/// <summary>
/// Transient Redis payload behind a single-use MFA challenge token.
/// Stored transiently (Redis); consumed exactly once on verification.
/// This is an UNVERIFIED challenge: it only authorizes factor verification,
/// never a sensitive mutation. Verified proofs use <c>StepUpProofPayload</c>.
/// </summary>
public sealed record MfaChallengePayload(
    Guid ChallengeId,
    Guid UserId,
    MfaChallengePurpose Purpose,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    Guid? SessionId = null);
