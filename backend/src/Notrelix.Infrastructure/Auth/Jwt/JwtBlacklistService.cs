using System.Globalization;
using Notrelix.Application.Common.Exceptions;

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
        return await GuardReadAsync(() => _redis.GetDatabase().KeyExistsAsync(BuildKey(jti)));
    }

    public async Task<DateTimeOffset?> GetUserRevokedBeforeAsync(Guid userId)
    {
        var value = await GuardReadAsync(() => _redis.GetDatabase().StringGetAsync(BuildUserWatermarkKey(userId)));
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

    public async Task<DateTimeOffset?> GetSessionRevokedBeforeAsync(Guid sessionId)
    {
        var value = await GuardReadAsync(() => _redis.GetDatabase().StringGetAsync(BuildSessionWatermarkKey(sessionId)));
        if (!value.HasValue)
            return null;

        return DateTimeOffset.TryParse(value.ToString(), null, DateTimeStyles.RoundtripKind, out var watermark)
            ? watermark
            : null;
    }

    public async Task RevokeSessionBeforeAsync(Guid sessionId, DateTimeOffset revokedBefore, TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero) return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            BuildSessionWatermarkKey(sessionId),
            revokedBefore.ToString("O", CultureInfo.InvariantCulture),
            expiration);
    }

    // A failed read must never read as "not revoked": it surfaces as a retryable 503 (fail closed).
    private static async Task<T> GuardReadAsync<T>(Func<Task<T>> read)
    {
        try
        {
            return await read();
        }
        catch (RedisException ex)
        {
            throw new RevocationStoreUnavailableException(ex);
        }
    }

    private static string BuildKey(string jti)
        => $"Notrelix_jwt:blacklist:{jti}";

    private static string BuildUserWatermarkKey(Guid userId)
        => $"auth:user-revoked-before:{userId}";

    private static string BuildSessionWatermarkKey(Guid sessionId)
        => $"auth:session-revoked-before:{sessionId}";
}
