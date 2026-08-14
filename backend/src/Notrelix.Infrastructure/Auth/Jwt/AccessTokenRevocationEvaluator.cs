namespace Notrelix.Infrastructure.Auth.Jwt;

public static class AccessTokenRevocationEvaluator
{
    public static bool ShouldReject(DateTimeOffset? tokenIssuedAt, DateTimeOffset? revokedBefore)
        => revokedBefore is not null
           && (tokenIssuedAt is null || tokenIssuedAt <= revokedBefore);
}