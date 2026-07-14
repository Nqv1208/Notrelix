using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class LogoutTests : IdentityHandlerTestBase
{
    private LogoutCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
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
}
