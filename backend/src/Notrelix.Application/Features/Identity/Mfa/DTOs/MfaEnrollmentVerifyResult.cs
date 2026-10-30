namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

/// <summary>
/// Plaintext one-time recovery codes. Returned exactly once, at activation
/// or regeneration; only hashes are ever persisted.
/// </summary>
public sealed record MfaEnrollmentVerifyResult(
    Guid MfaMethodId,
    IReadOnlyList<string> RecoveryCodes);

/// <summary>
/// The answer to activating a factor: the one-time recovery codes and how many
/// other devices the activation signed out.
/// </summary>
public sealed record MfaEnrollmentActivationResult(
    Guid MfaMethodId,
    IReadOnlyList<string> RecoveryCodes,
    int OtherSessionsEnded);
