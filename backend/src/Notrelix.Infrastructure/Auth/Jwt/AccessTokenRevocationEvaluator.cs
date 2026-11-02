namespace Notrelix.Infrastructure.Auth.Jwt;

public static class AccessTokenRevocationEvaluator
{
    public static bool ShouldReject(DateTimeOffset? tokenIssuedAt, DateTimeOffset? revokedBefore)
        => revokedBefore is not null
           && (tokenIssuedAt is null || tokenIssuedAt <= revokedBefore);

    /// <summary>
    /// The instant to compare with a revocation watermark. The precise millisecond claim is used only when it
    /// agrees with the signed whole-second <c>iat</c>; a token without it, or with a disagreeing one, keeps the
    /// whole-second value, which <see cref="ShouldReject"/> treats conservatively (<c>iat &lt;= watermark</c> rejects).
    /// </summary>
    public static DateTimeOffset? ResolveIssuedAt(string? issuedAtSeconds, string? issuedAtMilliseconds)
    {
        if (!long.TryParse(issuedAtSeconds, out var seconds))
        {
            return null;
        }

        if (long.TryParse(issuedAtMilliseconds, out var millis) && Math.DivRem(millis, 1000, out _) == seconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(millis);
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }
}
