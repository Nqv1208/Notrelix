using Notrelix.Application.Features.Identity.Auth.Throttling;

namespace Notrelix.Infrastructure.Identity.Auth;

/// <summary>
/// Redis implementation of <see cref="ILoginThrottle"/>. Counters and locks are
/// plain keys with a TTL (derived state, safe to lose on a cache flush only in
/// the sense that limits restart; never authoritative for any business fact).
/// Subject and IP are SHA-256 hashed into the key, so Redis never holds an
/// email address or an IP address. Every multi-key step is one Lua script, so
/// parallel guesses cannot slip past the limit between a read and a write.
/// A Redis failure surfaces as an exception (fail closed): the credential is
/// not verified without a working throttle.
/// </summary>
public sealed class RedisLoginThrottle : ILoginThrottle
{
    private const string KeyPrefix = Caching.RedisCacheService.KeyPrefix + "identity:throttle:";

    // KEYS: ipLock, subjectLock, subjectCount. ARGV: max, windowSec, lockSec.
    // Returns the remaining lock time in ms (0 = attempt reserved).
    private const string BeginScript = """
        local ipTtl = redis.call('PTTL', KEYS[1])
        local subjectTtl = redis.call('PTTL', KEYS[2])
        local ttl = math.max(ipTtl, subjectTtl)
        if ttl > 0 then return ttl end
        local n = redis.call('INCR', KEYS[3])
        if n == 1 then redis.call('EXPIRE', KEYS[3], ARGV[2]) end
        if n > tonumber(ARGV[1]) then
            redis.call('SET', KEYS[2], '1', 'EX', ARGV[3])
            redis.call('DEL', KEYS[3])
            return tonumber(ARGV[3]) * 1000
        end
        return 0
        """;

    // KEYS: subjectCount, subjectLock. ARGV: max, lockSec.
    private const string SubjectFailureScript = """
        local c = tonumber(redis.call('GET', KEYS[1]) or '0')
        if c >= tonumber(ARGV[1]) then
            redis.call('SET', KEYS[2], '1', 'EX', ARGV[2])
            redis.call('DEL', KEYS[1])
        end
        return 1
        """;

    // KEYS: ipCount, ipLock. ARGV: max, windowSec, lockSec.
    private const string IpFailureScript = """
        local n = redis.call('INCR', KEYS[1])
        if n == 1 then redis.call('EXPIRE', KEYS[1], ARGV[2]) end
        if n >= tonumber(ARGV[1]) then
            redis.call('SET', KEYS[2], '1', 'EX', ARGV[3])
            redis.call('DEL', KEYS[1])
        end
        return 1
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly IClientMetadata _client;
    private readonly LoginThrottleOptions _options;

    public RedisLoginThrottle(
        IConnectionMultiplexer redis,
        IClientMetadata client,
        IOptions<LoginThrottleOptions> options)
    {
        _redis = redis;
        _client = client;
        _options = options.Value;
    }

    public async Task<LoginThrottleDecision> BeginAttemptAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var retryMs = (long)await db.ScriptEvaluateAsync(
            BeginScript,
            new RedisKey[] { IpKey("lock"), SubjectKey(scope, subject, "lock"), SubjectKey(scope, subject, "count") },
            new RedisValue[]
            {
                _options.MaxFailuresPerAccount,
                Seconds(_options.AccountWindow),
                Seconds(_options.LockDuration)
            });

        return retryMs > 0
            ? LoginThrottleDecision.Locked(TimeSpan.FromMilliseconds(retryMs))
            : LoginThrottleDecision.Allowed;
    }

    public async Task RecordFailureAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.ScriptEvaluateAsync(
            SubjectFailureScript,
            new RedisKey[] { SubjectKey(scope, subject, "count"), SubjectKey(scope, subject, "lock") },
            new RedisValue[] { _options.MaxFailuresPerAccount, Seconds(_options.LockDuration) });

        await RecordIpFailureAsync(db);
    }

    public Task RecordAnonymousFailureAsync(CancellationToken cancellationToken = default)
        => RecordIpFailureAsync(_redis.GetDatabase());

    public async Task RecordSuccessAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default)
    {
        await _redis.GetDatabase().KeyDeleteAsync(SubjectKey(scope, subject, "count"));
    }

    public async Task ClearSubjectAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default)
    {
        await _redis.GetDatabase().KeyDeleteAsync(new RedisKey[]
        {
            SubjectKey(scope, subject, "count"),
            SubjectKey(scope, subject, "lock")
        });
    }

    private Task<RedisResult> RecordIpFailureAsync(IDatabase db)
        => db.ScriptEvaluateAsync(
            IpFailureScript,
            new RedisKey[] { IpKey("count"), IpKey("lock") },
            new RedisValue[]
            {
                _options.MaxFailuresPerIp,
                Seconds(_options.IpWindow),
                Seconds(_options.LockDuration)
            });

    private static long Seconds(TimeSpan value) => (long)Math.Ceiling(value.TotalSeconds);

    private string IpKey(string part) => $"{KeyPrefix}ip:{Hash(_client.IpAddress ?? "unknown")}:{part}";

    private static string SubjectKey(LoginThrottleScope scope, string subject, string part)
        => $"{KeyPrefix}{scope.ToString().ToLowerInvariant()}:{Hash(subject)}:{part}";

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

/// <summary>Startup validation so a bad value can never silently disable the lockout.</summary>
public sealed class LoginThrottleOptionsValidator : IValidateOptions<LoginThrottleOptions>
{
    public ValidateOptionsResult Validate(string? name, LoginThrottleOptions options)
    {
        var failures = new List<string>();

        if (options.MaxFailuresPerAccount < 1)
            failures.Add($"{LoginThrottleOptions.SectionName}:MaxFailuresPerAccount must be at least 1.");
        if (options.MaxFailuresPerIp < 1)
            failures.Add($"{LoginThrottleOptions.SectionName}:MaxFailuresPerIp must be at least 1.");
        if (options.AccountWindow < TimeSpan.FromSeconds(1))
            failures.Add($"{LoginThrottleOptions.SectionName}:AccountWindow must be at least one second.");
        if (options.IpWindow < TimeSpan.FromSeconds(1))
            failures.Add($"{LoginThrottleOptions.SectionName}:IpWindow must be at least one second.");
        if (options.LockDuration < TimeSpan.FromSeconds(1))
            failures.Add($"{LoginThrottleOptions.SectionName}:LockDuration must be at least one second.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
