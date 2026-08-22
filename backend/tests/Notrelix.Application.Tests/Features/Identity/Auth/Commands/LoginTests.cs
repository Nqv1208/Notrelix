using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class LoginTests : IdentityHandlerTestBase
{
    private LoginCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        PasswordHasherMock.Object,
        SessionIssuerMock.Object,
        ChallengeStoreMock.Object,
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
        result.TypedErrors.Should().Contain(e =>
            e.Code == "identity.auth.invalid-credentials" && e.Type == ApplicationErrorType.Authentication);
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
        result.TypedErrors.Should().Contain(e =>
            e.Code == "identity.auth.invalid-credentials" && e.Type == ApplicationErrorType.Authentication);
    }

    [Fact]
    public async Task Handle_WhenUserHasActiveMfaMethod_ReturnsChallengeAndIssuesNoSession()
    {
        var user = CreateUser();
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        var method = UserMfaMethod.Create(user.Id, MfaMethodType.AuthenticatorApp, TestNow, secretRef: SecretRef.Create("protected-secret"));
        method.Verify(TestNow);
        method.SetAsPrimary(TestNow);
        SetupUserMfaMethods(method);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = TestEmail, Password = TestPassword }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.MfaRequired.Should().BeTrue();
        result.Data.MfaChallengeToken.Should().NotBeNullOrWhiteSpace();
        result.Data.MfaMethod.Should().Be(nameof(MfaMethodType.AuthenticatorApp));
        result.Data.MfaExpiresAt.Should().BeAfter(TestNow.UtcDateTime);
        result.Data.AccessToken.Should().BeNull();
        SessionIssuerMock.Verify(s => s.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        ChallengeStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<MfaChallengePayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoActiveMfaMethod_IssuesSessionWithoutChallenge()
    {
        var user = CreateUser();
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        SetupUserMfaMethods();
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
        result.Data!.MfaRequired.Should().BeFalse();
        ChallengeStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<MfaChallengePayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}