using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class LogoutTests : IdentityHandlerTestBase
{
    private LogoutCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        JwtBlacklistMock.Object,
        DateTimeProviderMock.Object,
        LogoutLoggerMock.Object);

    [Fact]
    public async Task Handle_WhenSessionExists_RevokeSession()
    {
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupSessions(session);

        var sut = CreateSut();
        var result = await sut.Handle(new LogoutCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task Handle_WhenAccessTokenProvided_BlacklistsIt()
    {
        // Create a minimal valid JWT payload with jti and exp
        var payload = new { jti = Guid.NewGuid().ToString(), exp = (int)TestNow.AddHours(1).ToUnixTimeSeconds() };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var accessToken = $"header.{base64}.signature";

        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupSessions(session);

        var sut = CreateSut();
        var result = await sut.Handle(new LogoutCommand { RefreshToken = TestRefreshToken, AccessToken = accessToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        JwtBlacklistMock.Verify(b => b.BlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsSuccess()
    {
        SetupSessions();

        var sut = CreateSut();
        var result = await sut.Handle(new LogoutCommand { RefreshToken = "nonexistent-token" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTheRefreshCookieIsAnotherPersons_NeverEndsTheirSession()
    {
        var theirs = CreateSession(userId: Guid.CreateVersion7(), rawRefreshToken: "their-cookie");
        SetupSessions(theirs);

        var result = await CreateSut().Handle(new LogoutCommand { RefreshToken = "their-cookie" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        theirs.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task Handle_EndsThisDeviceOnly_AnotherOwnSessionStaysActive()
    {
        var thisDevice = CreateSession(rawRefreshToken: TestRefreshToken);
        var otherDevice = CreateSession(rawRefreshToken: "other-device-cookie");
        SetupSessions(thisDevice, otherDevice);
        RequestContextMock.Setup(c => c.SessionId).Returns(thisDevice.Id);

        await CreateSut().Handle(new LogoutCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        thisDevice.Status.Should().Be(SessionStatus.Revoked);
        otherDevice.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(otherDevice.Id, It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutARefreshCookie_TheSessionTheAccessCredentialNamesStillEnds()
    {
        var session = CreateSession();
        SetupSessions(session);
        RequestContextMock.Setup(c => c.SessionId).Returns(session.Id);

        var result = await CreateSut().Handle(new LogoutCommand { RefreshToken = "" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(session.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ASecondSignOut_FindsNothingActive_AndNeverRevivesTheSession()
    {
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        session.Revoke(TestNow);
        var revokedAt = session.RevokedAt;
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddMinutes(5));
        SetupSessions(session);

        var result = await CreateSut().Handle(new LogoutCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
        session.RevokedAt.Should().Be(revokedAt);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<Notrelix.Domain.Identity.Security.SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }
}
