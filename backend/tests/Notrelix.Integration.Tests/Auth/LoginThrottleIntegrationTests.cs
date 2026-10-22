using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Identity.Auth;
using Notrelix.Infrastructure.Identity.Mfa;
using Notrelix.Integration.Tests.Containers;
using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// Login and MFA lockout against the real PostgreSQL + Redis graph (D10):
/// per-account and per-IP counters, generic identical answers for known and
/// unknown accounts, reset on success, isolation between accounts and IPs,
/// lock expiry, and no email or IP stored in clear in Redis (NRX-003, NRX-012).
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class LoginThrottleIntegrationTests : IAsyncLifetime
{
    private const string GoodPassword = "Password1!";
    private const string GoodCode = "123456";
    private const int AccountMax = 3;
    private const int IpMax = 6;
    private const int RoomyIpMax = 100;

    private readonly CacheTestContainer _fixture;
    private ConnectionMultiplexer _redis = null!;
    private RedisCache _distributedCache = null!;
    private string _clientIp = "203.0.113.10";
    private TimeSpan _lock = TimeSpan.FromSeconds(30);
    private int _ipMax = RoomyIpMax;

    public LoginThrottleIntegrationTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        var config = ConfigurationOptions.Parse(_fixture.RedisConnectionString);
        config.AbortOnConnectFail = false;
        config.AllowAdmin = true;
        _redis = ConnectionMultiplexer.Connect(config);
        _distributedCache = new RedisCache(new RedisCacheOptions
        {
            Configuration = _fixture.RedisConnectionString,
            InstanceName = "Notrelix_",
        });

        await using var context = _fixture.CreatePostgresContext();
        context.Users.Add(User.Create("known@example.com", "Known", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true));
        context.Users.Add(User.Create("other@example.com", "Other", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true));
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }

    private RedisLoginThrottle CreateThrottle()
    {
        var client = new Mock<IClientMetadata>();
        client.Setup(c => c.IpAddress).Returns(() => _clientIp);
        return new RedisLoginThrottle(_redis, client.Object, Options.Create(new LoginThrottleOptions
        {
            MaxFailuresPerAccount = AccountMax,
            AccountWindow = TimeSpan.FromMinutes(1),
            MaxFailuresPerIp = _ipMax,
            IpWindow = TimeSpan.FromMinutes(1),
            LockDuration = _lock,
        }));
    }

    private async Task<Result<AuthResult>> LoginAsync(string email, string password)
    {
        await using var context = _fixture.CreatePostgresContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string plain, string _) => plain == GoodPassword);
        var issuer = new Mock<IAuthSessionIssuer>();
        issuer.Setup(i => i.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { AccessToken = "a", RefreshToken = "r" });
        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new LoginCommandHandler(
            context, hasher.Object, issuer.Object, Mock.Of<IMfaChallengeStore>(), CreateThrottle(), time.Object,
            NullLogger<LoginCommandHandler>.Instance);
        var result = await handler.Handle(new LoginCommand { Email = email, Password = password }, CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    private static string Code(Result<AuthResult> result) => result.TypedErrors.Single().Code;

    [Fact]
    public async Task Login_AfterMaxFailures_LocksEvenForTheRightPassword_WithRetryAfter()
    {
        for (var i = 0; i < AccountMax; i++)
        {
            var failed = await LoginAsync("known@example.com", "wrong");
            Code(failed).Should().Be("identity.auth.invalid-credentials");
        }

        var locked = await LoginAsync("known@example.com", GoodPassword);

        locked.Succeeded.Should().BeFalse();
        var error = locked.TypedErrors.Single();
        error.Code.Should().Be(LoginThrottleErrors.Code);
        error.Type.Should().Be(ApplicationErrorType.TooManyRequests);
        error.RetryAfter.Should().NotBeNull();
        error.RetryAfter!.Value.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(_lock);
    }

    [Fact]
    public async Task Login_UnknownAndKnownAccount_GetIdenticalFailureAndIdenticalLock()
    {
        var known = new List<ApplicationError>();
        var unknown = new List<ApplicationError>();

        for (var i = 0; i < AccountMax + 1; i++)
        {
            known.Add((await LoginAsync("known@example.com", "wrong")).TypedErrors.Single());
            unknown.Add((await LoginAsync("nobody@example.com", "wrong")).TypedErrors.Single());
        }

        known.Select(e => (e.Code, e.Message, e.Type)).Should().Equal(unknown.Select(e => (e.Code, e.Message, e.Type)));
        known.Take(AccountMax).Should().OnlyContain(e => e.Code == "identity.auth.invalid-credentials");
        known.Last().Code.Should().Be(LoginThrottleErrors.Code);
        unknown.Last().Code.Should().Be(LoginThrottleErrors.Code);
    }

    [Fact]
    public async Task Login_Success_ResetsTheAccountCounter()
    {
        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < AccountMax - 1; i++)
            {
                (await LoginAsync("known@example.com", "wrong")).Succeeded.Should().BeFalse();
            }

            (await LoginAsync("known@example.com", GoodPassword)).Succeeded.Should().BeTrue(
                $"round {round}: a success before the limit must reset the counter");
        }
    }

    [Fact]
    public async Task Login_LockOnOneAccount_DoesNotLockAnotherAccountOnTheSameIp()
    {
        for (var i = 0; i < AccountMax; i++)
            await LoginAsync("known@example.com", "wrong");
        (await LoginAsync("known@example.com", GoodPassword)).Succeeded.Should().BeFalse();

        (await LoginAsync("other@example.com", GoodPassword)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Login_IpLimitAcrossAccounts_LocksThatIpOnly()
    {
        _ipMax = IpMax;
        // IpMax failures spread over distinct emails so no account limit is reached.
        for (var i = 0; i < IpMax; i++)
            await LoginAsync($"spray{i}@example.com", "wrong");

        var sameIp = await LoginAsync("other@example.com", GoodPassword);
        sameIp.Succeeded.Should().BeFalse();
        Code(sameIp).Should().Be(LoginThrottleErrors.Code);

        _clientIp = "198.51.100.77";
        (await LoginAsync("other@example.com", GoodPassword)).Succeeded.Should().BeTrue("a different IP is unaffected");
    }

    [Fact]
    public async Task Login_SuccessDoesNotResetTheIpCounter()
    {
        _ipMax = IpMax;
        // An attacker owning one valid account must not be able to erase IP failures with its own success.
        for (var i = 0; i < IpMax - 1; i++)
        {
            await LoginAsync($"spray{i}@example.com", "wrong");
            (await LoginAsync("other@example.com", GoodPassword)).Succeeded.Should().BeTrue();
        }

        await LoginAsync("spray-final@example.com", "wrong");

        Code(await LoginAsync("other@example.com", GoodPassword)).Should().Be(LoginThrottleErrors.Code);
    }

    [Fact]
    public async Task Login_LockExpires()
    {
        _lock = TimeSpan.FromSeconds(2);
        for (var i = 0; i < AccountMax; i++)
            await LoginAsync("known@example.com", "wrong");
        Code(await LoginAsync("known@example.com", GoodPassword)).Should().Be(LoginThrottleErrors.Code);

        await Task.Delay(TimeSpan.FromSeconds(2.5));

        (await LoginAsync("known@example.com", GoodPassword)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Login_ParallelGuesses_CannotExceedTheLimit()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => LoginAsync("known@example.com", GoodPassword + "x")));

        results.Count(r => Code(r) == "identity.auth.invalid-credentials")
            .Should().BeLessThanOrEqualTo(AccountMax, "the attempt is reserved atomically before the password is verified");
    }

    [Fact]
    public async Task Redis_NeverHoldsEmailOrIpInClear()
    {
        await LoginAsync("known@example.com", "wrong");

        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "*throttle*").Select(k => k.ToString()).ToArray();

        keys.Should().NotBeEmpty();
        keys.Should().OnlyContain(k => !k.Contains("example.com") && !k.Contains("known") && !k.Contains(_clientIp));
    }

    // ── MFA challenge step ─────────────────────────────────────────

    private async Task<(string Token, Guid UserId)> NewChallengeAsync(string email)
    {
        await using var context = _fixture.CreatePostgresContext();
        var user = context.Users.Single(u => u.NormalizedEmail == email);
        var store = new MfaChallengeStore(new RedisCacheService(_distributedCache, _redis));
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        await store.StoreAsync(
            token,
            new MfaChallengePayload(Guid.CreateVersion7(), user.Id, MfaChallengePurpose.PasswordLogin, now, now.Add(MfaPolicy.ChallengeTtl)),
            MfaPolicy.ChallengeTtl,
            CancellationToken.None);
        return (token, user.Id);
    }

    private async Task<Result<AuthResult>> CompleteAsync(string token, string code)
    {
        await using var context = _fixture.CreatePostgresContext();
        var verifier = new Mock<IMfaCodeVerifier>();
        verifier.Setup(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string c, DateTimeOffset _, CancellationToken _) => c == GoodCode);
        var issuer = new Mock<IAuthSessionIssuer>();
        issuer.Setup(i => i.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { AccessToken = "a", RefreshToken = "r" });
        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new CompleteMfaChallengeCommandHandler(
            context, new MfaChallengeStore(new RedisCacheService(_distributedCache, _redis)), verifier.Object,
            CreateThrottle(), issuer.Object, time.Object, NullLogger<CompleteMfaChallengeCommandHandler>.Instance);
        var result = await handler.Handle(new CompleteMfaChallengeCommand { ChallengeToken = token, Code = code }, CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task Mfa_WrongCodesAcrossChallenges_LockTheAccount_EvenForTheRightCode()
    {
        for (var i = 0; i < AccountMax; i++)
        {
            var (token, _) = await NewChallengeAsync("known@example.com");
            Code(await CompleteAsync(token, "000000")).Should().Be("identity.mfa.invalid-code");
        }

        var (freshToken, _) = await NewChallengeAsync("known@example.com");
        var locked = await CompleteAsync(freshToken, GoodCode);

        locked.Succeeded.Should().BeFalse();
        Code(locked).Should().Be(LoginThrottleErrors.Code);
        locked.TypedErrors.Single().RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Mfa_Success_ResetsTheCounter_AndOtherAccountIsIsolated()
    {
        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < AccountMax - 1; i++)
            {
                var (bad, _) = await NewChallengeAsync("known@example.com");
                (await CompleteAsync(bad, "000000")).Succeeded.Should().BeFalse();
            }

            var (good, _) = await NewChallengeAsync("known@example.com");
            (await CompleteAsync(good, GoodCode)).Succeeded.Should().BeTrue();
        }

        for (var i = 0; i < AccountMax; i++)
        {
            var (bad, _) = await NewChallengeAsync("known@example.com");
            await CompleteAsync(bad, "000000");
        }

        var (otherToken, _) = await NewChallengeAsync("other@example.com");
        (await CompleteAsync(otherToken, GoodCode)).Succeeded.Should().BeTrue("another account is not locked");
    }

    [Fact]
    public async Task Mfa_UnknownChallengeTokens_CountAgainstTheIpOnly()
    {
        _ipMax = IpMax;
        for (var i = 0; i < IpMax; i++)
        {
            Code(await CompleteAsync($"not-a-real-token-{i}", GoodCode)).Should().Be("identity.mfa.challenge-invalid");
        }

        var (token, _) = await NewChallengeAsync("other@example.com");
        Code(await CompleteAsync(token, GoodCode)).Should().Be(LoginThrottleErrors.Code);

        _clientIp = "198.51.100.88";
        (await CompleteAsync(token, GoodCode)).Succeeded.Should().BeTrue();
    }
}
