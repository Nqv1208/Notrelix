using Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class RefreshTokenTests : IdentityHandlerTestBase
{
    private RefreshTokenCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        JwtServiceMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenValidSession_ReturnsNewTokens()
    {
        var user = CreateUser();
        var session = CreateSession(rawRefreshToken: TestRefreshToken);
        SetupUsers(user);
        SetupSessions(session);

        JwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("new-access-token");
        JwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = TestRefreshToken }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");
        session.Status.Should().Be(SessionStatus.Revoked);
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
}
