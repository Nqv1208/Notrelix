namespace Notrelix.Application.Features.Identity.Mfa.Abstractions;

/// <summary>
/// Verifies an MFA code (TOTP or recovery code) against the user's enrolled
/// active factors. Recovery code consumption mutates the recovery batch.
/// </summary>
public interface IMfaCodeVerifier
{
    Task<bool> VerifyAsync(Guid userId, string code, DateTimeOffset now, CancellationToken ct);
}