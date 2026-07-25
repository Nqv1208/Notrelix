using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class LoginTests : IdentityHandlerTestBase
{
    private LoginCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        PasswordHasherMock.Object,
        SessionIssuerMock.Object,
        DateTimeProviderMock.Object,
        LoginLoggerMock.Object);

    [Fact]
    public async Task Handle_WhenValidCredentials_ReturnsSuccess()
    {
        var user = CreateUser();
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        SessionIssuerMock.Setup(s => s.IssueAsync(user, TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = TestNow.AddHours(1).UtcDateTime,
                User = new UserDto { Id = TestUserId, Email = TestEmail, Name = "Test User", EmailConfirmed = false }
            });

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        SetupUsers();
        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ReturnsFailure()
    {
        var user = CreateUser(status: UserStatus.Inactive);
        SetupUsers(user);
        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("deactivated"));
    }

    [Fact]
    public async Task Handle_WhenUserSuspended_ReturnsFailure()
    {
        var user = CreateUser(status: UserStatus.Suspended);
        SetupUsers(user);
        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("suspended"));
    }

    [Fact]
    public async Task Handle_WhenPasswordWrong_ReturnsFailure()
    {
        var user = CreateUser();
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(false);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
    }
}
