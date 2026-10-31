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
}
