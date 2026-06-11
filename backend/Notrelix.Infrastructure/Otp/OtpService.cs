using System.Security.Cryptography;
using StackExchange.Redis;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Otp;

public class OtpService : IOtpService
{
    private readonly IConnectionMultiplexer _redis;

    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AttemptsTtl = TimeSpan.FromMinutes(15);
    private const int MaxAttempts = 5;

    public OtpService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string> GenerateAsync(string purpose, string identifier)
    {
        var db = _redis.GetDatabase();
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var key = BuildKey(purpose, identifier);

        await db.StringSetAsync(key, code, OtpTtl);

        var attemptsKey = BuildAttemptsKey(purpose, identifier);
        await db.KeyDeleteAsync(attemptsKey);

        return code;
    }

    public async Task<bool> ValidateAsync(string purpose, string identifier, string code)
    {
        var db = _redis.GetDatabase();
        var attemptsKey = BuildAttemptsKey(purpose, identifier);

        var attempts = await db.StringIncrementAsync(attemptsKey);
        if (attempts == 1)
            await db.KeyExpireAsync(attemptsKey, AttemptsTtl);

        if (attempts > MaxAttempts)
        {
            await db.KeyDeleteAsync(BuildKey(purpose, identifier));
            return false;
        }

        var key = BuildKey(purpose, identifier);
        var stored = await db.StringGetAsync(key);

        if (!stored.HasValue || stored.ToString() != code)
            return false;

        await db.KeyDeleteAsync(key);
        await db.KeyDeleteAsync(attemptsKey);
        return true;
    }

    public async Task<int> GetAttemptsAsync(string purpose, string identifier)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(BuildAttemptsKey(purpose, identifier));
        return value.HasValue ? (int)value : 0;
    }

    private static string BuildKey(string purpose, string identifier)
        => $"Notrelix_otp:{purpose}:{identifier.ToLowerInvariant()}";

    private static string BuildAttemptsKey(string purpose, string identifier)
        => $"Notrelix_otp:attempts:{purpose}:{identifier.ToLowerInvariant()}";
}
