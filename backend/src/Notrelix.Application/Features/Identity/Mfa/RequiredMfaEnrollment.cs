using System.Security.Cryptography;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa;

/// <summary>
/// The sign-in answer for a person whose Account requires MFA and who has no factor: no session, only a
/// short-lived single-use token that opens the required-enrollment step.
/// </summary>
internal static class RequiredMfaEnrollment
{
    public static async Task<AuthResult> ChallengeAsync(
        IMfaChallengeStore store, Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var payload = new MfaChallengePayload(
            Guid.CreateVersion7(), userId, MfaChallengePurpose.RequiredEnrollment, now, now.Add(MfaPolicy.EnrollmentTtl));
        await store.StoreAsync(token, payload, MfaPolicy.EnrollmentTtl, ct);

        return new AuthResult
        {
            MfaEnrollmentRequired = true,
            MfaEnrollmentToken = token,
            MfaMethod = nameof(Notrelix.Domain.Identity.Mfa.MfaMethodType.AuthenticatorApp),
            MfaExpiresAt = payload.ExpiresAt.UtcDateTime
        };
    }
}
