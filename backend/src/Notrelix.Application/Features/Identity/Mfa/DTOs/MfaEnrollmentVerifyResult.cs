namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

/// <summary>
/// Plaintext one-time recovery codes. Returned exactly once, at activation
/// or regeneration; only hashes are ever persisted.
/// </summary>
public sealed record MfaEnrollmentVerifyResult(
    Guid MfaMethodId,
    IReadOnlyList<string> RecoveryCodes);
