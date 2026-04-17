using StackExchange.Redis;
using TodoApp.Application.Common.Interfaces;

namespace TodoApp.Infrastructure.Jwt;

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

    private static string BuildKey(string jti)
        => $"TodoApp_jwt:blacklist:{jti}";
}
