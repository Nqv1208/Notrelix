namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// TOTP (RFC 6238) mechanics for the MFA authenticator-app factor.
/// The service owns secret generation, at-rest protection, and code verification.
/// </summary>
public interface IMfaTotpService
{
    /// <summary>Generates a new random base32-encoded TOTP secret.</summary>
    string GenerateSecretKey();

    /// <summary>Builds an otpauth:// URI for authenticator app enrollment.</summary>
    string BuildOtpAuthUri(string base32Secret, string accountName, string issuer);

    /// <summary>
    /// Verifies a 6-digit TOTP code for the given secret within the allowed
    /// time-step window. Returns false for empty/expired/invalid codes.
    /// </summary>
    bool VerifyCode(string base32Secret, string code, DateTimeOffset now);

    /// <summary>Protects the raw base32 secret for storage at rest.</summary>
    string ProtectSecret(string base32Secret);

    /// <summary>Reverses <see cref="ProtectSecret"/>.</summary>
    string UnprotectSecret(string protectedSecret);
}
