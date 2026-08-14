using System.Globalization;

namespace Notrelix.Infrastructure.Auth.Jwt;

public class JwtBlacklistService : IJwtBlacklistService
{
    private readonly IConnectionMultiplexer _redis;

    public JwtBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task BlacklistAsync(string jti, TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero) return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(BuildKey(jti), "1", expiration);
    }

    public async Task<bool> IsBlacklistedAsync(string jti)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(BuildKey(jti));
    }

    public async Task<DateTimeOffset?> GetUserRevokedBeforeAsync(Guid userId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(BuildUserWatermarkKey(userId));
        if (!value.HasValue)
            return null;

        return DateTimeOffset.TryParse(value.ToString(), null, DateTimeStyles.RoundtripKind, out var watermark)
            ? watermark
            : null;
    }

    public async Task RevokeUserBeforeAsync(Guid userId, DateTimeOffset revokedBefore, TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero) return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            BuildUserWatermarkKey(userId),
            revokedBefore.ToString("O", CultureInfo.InvariantCulture),
            expiration);
    }

    private static string BuildKey(string jti)
        => $"Notrelix_jwt:blacklist:{jti}";

    private static string BuildUserWatermarkKey(Guid userId)
        => $"auth:user-revoked-before:{userId}";
}
