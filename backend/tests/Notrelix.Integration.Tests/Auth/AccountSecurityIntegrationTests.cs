using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Queries.GetAccountSecuritySummary;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Application.Features.Identity.Sessions.Commands.RevokeSession;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Identity.Auth;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// The account security page contract against real PostgreSQL + Redis: step-up and
/// change-password are rate limited per user (separately from sign-in), a user can never revoke
/// or count another user's sessions, and the summary reports the stored password-change time
/// without exposing secrets (NRX-003, NRX-012).
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class AccountSecurityIntegrationTests : IAsyncLifetime
{
    private const string GoodPassword = "Password1!";
    private const int AccountMax = 3;

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly CacheTestContainer _fixture;
    private ConnectionMultiplexer _redis = null!;
    private User _alice = null!;
    private User _bob = null!;
    private Guid _aliceSession;
    private Guid _bobSession;

    public AccountSecurityIntegrationTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        var config = ConfigurationOptions.Parse(_fixture.RedisConnectionString);
        config.AbortOnConnectFail = false;
        _redis = ConnectionMultiplexer.Connect(config);

        _alice = User.Create("alice-sec@example.com", "Alice", "alice-hash", Now.AddDays(-5), hasPasswordCredential: true);
        _bob = User.Create("bob-sec@example.com", "Bob", "bob-hash", Now.AddDays(-5), hasPasswordCredential: true);
        var aliceSession = UserSession.Create(_alice.Id, RefreshTokenHash.Create("alice-refresh"), Now.AddDays(30), Now.AddDays(-1));
        var aliceOther = UserSession.Create(_alice.Id, RefreshTokenHash.Create("alice-refresh-2"), Now.AddDays(30), Now.AddDays(-2));
        var bobSession = UserSession.Create(_bob.Id, RefreshTokenHash.Create("bob-refresh"), Now.AddDays(30), Now.AddDays(-1));
        _aliceSession = aliceSession.Id;
        _bobSession = bobSession.Id;

        await using var context = _fixture.CreatePostgresContext();
        context.Users.AddRange(_alice, _bob);
        context.Sessions.AddRange(aliceSession, aliceOther, bobSession);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }

    private RedisLoginThrottle CreateThrottle(string ip = "203.0.113.50")
    {
        var client = new Mock<IClientMetadata>();
        client.Setup(c => c.IpAddress).Returns(ip);
        return new RedisLoginThrottle(_redis, client.Object, Options.Create(new LoginThrottleOptions
        {
            MaxFailuresPerAccount = AccountMax,
            AccountWindow = TimeSpan.FromMinutes(1),
            MaxFailuresPerIp = 1000,
            IpWindow = TimeSpan.FromMinutes(1),
            LockDuration = TimeSpan.FromSeconds(30),
        }));
    }

    private static Mock<IDateTimeProvider> Clock()
    {
        var time = new Mock<IDateTimeProvider>();
        time.Setup(t => t.UtcNow).Returns(Now);
        return time;
    }

    private static Mock<IPasswordHasher> Hasher()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string plain, string _) => plain == GoodPassword);
        hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("new-hash");
        return hasher;
    }

    private SecurityStepUpService StepUp(Notrelix.Infrastructure.Data.ApplicationDbContext context, ILoginThrottle throttle)
    {
        var clock = FakeDateTimeProvider.WithFixedTime(Now);
        return new SecurityStepUpService(
            context, new InMemoryMfaChallengeStore(clock), new InMemoryStepUpProofStore(clock),
            Mock.Of<IMfaCodeVerifier>(), Hasher().Object, clock, throttle);
    }

    private async Task<Result<StepUpProofResult>> StepUpPasswordAsync(User user, string password)
    {
        await using var context = _fixture.CreatePostgresContext();
        return await StepUp(context, CreateThrottle())
            .CompletePasswordAsync(user.Id, Guid.CreateVersion7(), StepUpPurpose.DisableMfa, password, CancellationToken.None);
    }

    private static string Code(Result result) => result.TypedErrors.Single().Code;

    [Fact]
    public async Task StepUpPassword_AfterMaxWrongGuesses_LocksEvenForTheRightPassword_WithRetryAfter()
    {
        for (var i = 0; i < AccountMax; i++)
            Code(await StepUpPasswordAsync(_alice, "wrong")).Should().Be("identity.security.step-up-invalid-password");

        var locked = await StepUpPasswordAsync(_alice, GoodPassword);

        locked.Succeeded.Should().BeFalse();
        var error = locked.TypedErrors.Single();
        error.Code.Should().Be(LoginThrottleErrors.Code);
        error.Type.Should().Be(ApplicationErrorType.TooManyRequests);
        error.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task StepUpPassword_LockOnOneUser_DoesNotLockAnotherUserOnTheSameAddress()
    {
        for (var i = 0; i < AccountMax; i++)
            await StepUpPasswordAsync(_alice, "wrong");
        (await StepUpPasswordAsync(_alice, GoodPassword)).Succeeded.Should().BeFalse();

        (await StepUpPasswordAsync(_bob, GoodPassword)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task StepUpPassword_SuccessBeforeTheLimit_ResetsTheBudget()
    {
        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < AccountMax - 1; i++)
                (await StepUpPasswordAsync(_alice, "wrong")).Succeeded.Should().BeFalse();
            (await StepUpPasswordAsync(_alice, GoodPassword)).Succeeded.Should().BeTrue($"round {round}");
        }
    }

    [Fact]
    public async Task StepUpMfa_AfterMaxBadAttempts_IsLocked_WithoutLockingPasswordReverificationOrSignIn()
    {
        await using var context = _fixture.CreatePostgresContext();
        var service = StepUp(context, CreateThrottle());

        for (var i = 0; i < AccountMax; i++)
        {
            var bad = await service.CompleteMfaAsync(_alice.Id, Guid.CreateVersion7(), StepUpPurpose.DisableMfa, "no-such-challenge", "000000", CancellationToken.None);
            Code(bad).Should().Be("identity.security.step-up-invalid");
        }

        var locked = await service.CompleteMfaAsync(_alice.Id, Guid.CreateVersion7(), StepUpPurpose.DisableMfa, "no-such-challenge", "000000", CancellationToken.None);
        Code(locked).Should().Be(LoginThrottleErrors.Code);

        (await StepUpPasswordAsync(_alice, GoodPassword)).Succeeded.Should().BeTrue("the password budget is a separate counter");
        (await CreateThrottle().BeginAttemptAsync(LoginThrottleScope.PasswordLogin, _alice.NormalizedEmail)).IsLocked
            .Should().BeFalse("the sign-in counter is independent of step-up");
    }

    private async Task<Result> ChangePasswordAsync(User user, string current)
    {
        await using var context = _fixture.CreatePostgresContext();
        var throttle = CreateThrottle();
        var handler = new ChangePasswordCommandHandler(
            context, new FakeCurrentRequestContext().AsUser(user.Id), Hasher().Object, Mock.Of<IJwtBlacklistService>(),
            Mock.Of<IEmailService>(), Clock().Object, NullLogger<ChangePasswordCommandHandler>.Instance,
            StepUp(context, throttle), throttle, Mock.Of<Notrelix.Application.Features.Identity.Security.Abstractions.ISecurityActivityRecorder>());
        var result = await handler.Handle(
            new ChangePasswordCommand { CurrentPassword = current, NewPassword = "NewPassword123!" }, CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task ChangePassword_ThrottlesWrongCurrentPasswords_AndNeverChangesTheHashWhileLocked()
    {
        for (var i = 0; i < AccountMax; i++)
            Code(await ChangePasswordAsync(_alice, "wrong")).Should().Be("identity.auth.invalid-current-password");

        var locked = await ChangePasswordAsync(_alice, GoodPassword);

        Code(locked).Should().Be(LoginThrottleErrors.Code);
        await using var verify = _fixture.CreatePostgresContext();
        (await verify.Users.SingleAsync(u => u.Id == _alice.Id)).PasswordHash.Should().Be("alice-hash");
        (await verify.UserSecuritySettings.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ChangePassword_SharesTheGuessBudgetWithStepUpPassword()
    {
        await StepUpPasswordAsync(_alice, "wrong");
        await StepUpPasswordAsync(_alice, "wrong");
        Code(await ChangePasswordAsync(_alice, "wrong")).Should().Be("identity.auth.invalid-current-password");

        Code(await ChangePasswordAsync(_alice, GoodPassword)).Should().Be(LoginThrottleErrors.Code);
    }

    private async Task<Result<AccountSecuritySummaryDto>> SummaryAsync(User user)
    {
        await using var context = _fixture.CreatePostgresContext();
        var handler = new GetAccountSecuritySummaryQueryHandler(
            context, new FakeCurrentRequestContext().AsUser(user.Id), Clock().Object);
        return await handler.Handle(new GetAccountSecuritySummaryQuery(), CancellationToken.None);
    }

    [Fact]
    public async Task Summary_ReportsOwnSessionsOnly_AndPasswordChangeTimeAfterAChange()
    {
        var before = (await SummaryAsync(_alice)).Data!;
        before.ActiveSessionCount.Should().Be(2);
        before.PasswordChangedAt.Should().BeNull();
        before.HasPasswordCredential.Should().BeTrue();
        before.Mfa.IsEnabled.Should().BeFalse();
        (await SummaryAsync(_bob)).Data!.ActiveSessionCount.Should().Be(1);

        (await ChangePasswordAsync(_alice, GoodPassword)).Succeeded.Should().BeTrue();

        var after = (await SummaryAsync(_alice)).Data!;
        after.PasswordChangedAt.Should().Be(Now);
        after.ActiveSessionCount.Should().Be(0, "a password change revokes every session");
        (await SummaryAsync(_bob)).Data!.PasswordChangedAt.Should().BeNull();
        System.Text.Json.JsonSerializer.Serialize(after).Should().NotContain("alice-hash").And.NotContain("new-hash");
    }

    private async Task<Result> RevokeAsync(User actor, Guid sessionId, Guid? currentSessionId = null)
    {
        await using var context = _fixture.CreatePostgresContext();
        var current = new FakeCurrentRequestContext().AsUser(actor.Id);
        current.SessionId = currentSessionId;
        var handler = new RevokeSessionCommandHandler(
            context, current, Mock.Of<IJwtBlacklistService>(), Clock().Object, Mock.Of<Notrelix.Application.Features.Identity.Security.Abstractions.ISecurityActivityRecorder>(), NullLogger<RevokeSessionCommandHandler>.Instance);
        var result = await handler.Handle(new RevokeSessionCommand { SessionId = sessionId }, CancellationToken.None);
        await context.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task RevokeSession_OfAnotherUser_IsNotFound_AndTheSessionStaysActive()
    {
        var result = await RevokeAsync(_alice, _bobSession);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Single().Type.Should().Be(ApplicationErrorType.NotFound);
        await using var verify = _fixture.CreatePostgresContext();
        (await verify.Sessions.SingleAsync(s => s.Id == _bobSession)).Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task RevokeSession_OfAnUnknownId_IsNotFound_IndistinguishableFromAnotherUsersSession()
    {
        var unknown = await RevokeAsync(_alice, Guid.CreateVersion7());
        var foreign = await RevokeAsync(_alice, _bobSession);

        (unknown.TypedErrors.Single().Code, unknown.TypedErrors.Single().Message)
            .Should().Be((foreign.TypedErrors.Single().Code, foreign.TypedErrors.Single().Message));
    }

    [Fact]
    public async Task RevokeSession_OfOwnOtherSession_RevokesItAndKeepsTheCurrentOne()
    {
        await using (var seed = _fixture.CreatePostgresContext())
        {
            var other = await seed.Sessions.Where(s => s.UserId == _alice.Id && s.Id != _aliceSession).SingleAsync();
            (await RevokeAsync(_alice, other.Id, currentSessionId: _aliceSession)).Succeeded.Should().BeTrue();
        }

        await using var verify = _fixture.CreatePostgresContext();
        (await verify.Sessions.SingleAsync(s => s.Id == _aliceSession)).Status.Should().Be(SessionStatus.Active);
        (await verify.Sessions.CountAsync(s => s.UserId == _alice.Id && s.Status == SessionStatus.Revoked)).Should().Be(1);
        (await SummaryAsync(_alice)).Data!.ActiveSessionCount.Should().Be(1);
    }
}
