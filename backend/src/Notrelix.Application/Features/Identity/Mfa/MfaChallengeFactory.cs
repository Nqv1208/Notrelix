using System.Security.Cryptography;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa;

internal static class MfaChallengeFactory
{
    public static async Task<(string Token, MfaChallengePayload Payload)> CreateAsync(
        IMfaChallengeStore store,
        Guid userId,
        MfaChallengePurpose purpose,
        DateTimeOffset now,
        CancellationToken ct,
        Guid? sessionId = null)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var payload = new MfaChallengePayload(userId, purpose, now, now.Add(MfaPolicy.ChallengeTtl), sessionId);
        await store.StoreAsync(token, payload, MfaPolicy.ChallengeTtl, ct);
        return (token, payload);
    }
}
