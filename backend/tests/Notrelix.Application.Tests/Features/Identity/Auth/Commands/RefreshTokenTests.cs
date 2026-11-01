using Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RefreshTokenTests : IdentityHandlerTestBase
{
    private readonly Mock<IRefreshRotationLedger> _ledger = new();

    public RefreshTokenTests()
    {
        _ledger.Setup(l => l.TryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private RefreshTokenCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        JwtServiceMock.Object,
        JwtBlacklistMock.Object,
        _ledger.Object,
        ActivityMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenValidSession_ReturnsNewTokens()
    {
        var user = CreateUser();
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupUsers(user);
        SetupSessions(session);

        JwtServiceMock.Setup(j => j.GenerateAccessToken(user, It.IsAny<Guid?>())).Returns("new-access-token");
        JwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");
        // The session keeps its id and stays active; only its credential is replaced.
        session.Status.Should().Be(SessionStatus.Active);
        session.RefreshTokenHash.Should().Be(RefreshTokenHash.Create("new-refresh-token"));
        session.PreviousRefreshTokenHash.Should().Be(RefreshTokenHash.Create(TestRefreshToken).Hash);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsFailure()
    {
        SetupSessions();

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = "nonexistent-token" }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid or expired"));
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_RevokesSessionAndFails()
    {
        var user = CreateUser(status: UserStatus.Inactive);
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupUsers(user);
        SetupSessions(session);

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid or expired"));
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task Handle_WhenUserIsSuspended_RevokesSessionAndFails()
    {
        var user = CreateUser(status: UserStatus.Suspended);
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupUsers(user);
        SetupSessions(session);

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid or expired"));
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    /// <summary>A session whose credential TestRefreshToken was replaced by "successor" the given seconds ago.</summary>
    private UserSession RotatedSession(int secondsAgo)
    {
        var session = UserSession.Create(
            TestUserId, RefreshTokenHash.Create(TestRefreshToken), TestNow.AddDays(30), TestNow.AddMinutes(-5));
        session.Rotate(RefreshTokenHash.Create("successor"), TestNow.AddSeconds(-secondsAgo));
        return session;
    }

    [Fact]
    public async Task Handle_WhenTheClaimHolderFailedBeforeItsCommit_TheRetryTakesTheClaimOverAndRotates()
    {
        // The first request took the claim and its commit was refused, so the session still holds the
        // credential. The person's retry must rotate instead of waiting out the claim and signing out.
        var user = CreateUser();
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupUsers(user);
        SetupSessions(session);
        JwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _ledger.SetupSequence(l => l.TryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RefreshToken.Should().Be("new-refresh-token");
        session.RefreshTokenHash.Should().Be(RefreshTokenHash.Create("new-refresh-token"));
        _ledger.Verify(l => l.ReleaseClaimAsync(TestRefreshTokenHash, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheReplacedCredentialIsPresentedWithinTheGrace_ServesTheSuccessor()
    {
        var user = CreateUser();
        var session = RotatedSession(3);
        SetupUsers(user);
        SetupSessions(session);
        JwtServiceMock.Setup(j => j.GenerateAccessToken(user, It.IsAny<Guid?>())).Returns("access");
        _ledger.Setup(l => l.RecallSuccessorAsync(TestRefreshToken, It.IsAny<CancellationToken>())).ReturnsAsync("successor");

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RefreshToken.Should().Be("successor");
        session.Status.Should().Be(SessionStatus.Active);
        _ledger.Verify(l => l.TryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheGraceServedSuccessorIsNotTheCurrentCredential_Fails()
    {
        var session = RotatedSession(3);
        SetupUsers(CreateUser());
        SetupSessions(session);
        _ledger.Setup(l => l.RecallSuccessorAsync(TestRefreshToken, It.IsAny<CancellationToken>())).ReturnsAsync("someone-elses");

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTheReplacedCredentialComesBackAfterTheGrace_EndsTheSession()
    {
        var session = RotatedSession(11);
        SetupUsers(CreateUser());
        SetupSessions(session);

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        session.Status.Should().Be(SessionStatus.Revoked);
        _ledger.Verify(l => l.RecallSuccessorAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheSessionExpired_FailsAndDoesNotRotate()
    {
        var session = CreateSession(rawRefreshToken: TestRefreshToken, expiresAt: TestNow.AddSeconds(1));
        var later = TestNow.AddMinutes(1);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(later);
        SetupUsers(CreateUser());
        SetupSessions(session);

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        session.RefreshTokenHash.Should().Be(RefreshTokenHash.Create(TestRefreshToken));
        _ledger.Verify(l => l.TryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NearTheAbsoluteLifetime_CapsTheNewExpiryAtNinetyDaysAfterSignIn()
    {
        var signedInAt = TestNow.AddDays(-89);
        var session = UserSession.Create(TestUserId, RefreshTokenHash.Create(TestRefreshToken), TestNow.AddDays(5), signedInAt);
        SetupUsers(CreateUser());
        SetupSessions(session);
        JwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");

        var result = await CreateSut().Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.ExpiresAt.Should().Be(signedInAt.Add(SessionLifetimePolicy.AbsoluteLifetime));
        session.ExpiresAt.Should().BeBefore(TestNow.Add(SessionLifetimePolicy.IdleLifetime));
    }
}
