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
    public async Task Handle_WhenUserNotFound_ReturnsGenericFailureAndStillVerifiesPassword()
    {
        SetupUsers();
        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
        PasswordHasherMock.Verify(h => h.VerifyPassword(TestPassword, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ReturnsGenericFailureAndIssuesNoSession()
    {
        var user = CreateUser(status: UserStatus.Inactive);
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
        result.Errors.Should().NotContain(e => e.Contains("deactivated") || e.Contains("suspended"));
        SessionIssuerMock.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserSuspended_ReturnsGenericFailureAndIssuesNoSession()
    {
        var user = CreateUser(status: UserStatus.Suspended);
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid email or password"));
        result.Errors.Should().NotContain(e => e.Contains("deactivated") || e.Contains("suspended"));
        SessionIssuerMock.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
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
