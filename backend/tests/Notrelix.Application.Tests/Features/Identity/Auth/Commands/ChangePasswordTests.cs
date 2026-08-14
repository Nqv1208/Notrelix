using Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ChangePasswordTests : IdentityHandlerTestBase
{
    private ChangePasswordCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        PasswordHasherMock.Object,
        JwtBlacklistMock.Object,
        EmailServiceMock.Object,
        DateTimeProviderMock.Object,
        ChangePasswordLoggerMock.Object);

    [Fact]
    public async Task Handle_WhenCurrentPasswordValid_HashesNewPasswordAndRevokesSessions()
    {
        var user = CreateUser();
        var session = CreateSession();
        SetupUsers(user);
        SetupSessions(session);

        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        PasswordHasherMock.Setup(h => h.HashPassword("NewPassword123!")).Returns("new-hashed-password");

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hashed-password");
        session.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(
            j => j.RevokeUserBeforeAsync(TestUserId, TestNow, It.IsAny<TimeSpan>()),
            Times.Once);
        EmailServiceMock.Verify(e => e.SendAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_ReturnsFailureWithoutMutation()
    {
        var user = CreateUser();
        var session = CreateSession();
        SetupUsers(user);
        SetupSessions(session);

        PasswordHasherMock.Setup(h => h.VerifyPassword("wrong-password", TestHashedPassword)).Returns(false);

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = "wrong-password",
            NewPassword = "NewPassword123!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Current password is incorrect"));
        user.PasswordHash.Should().Be(TestHashedPassword);
        session.Status.Should().Be(SessionStatus.Active);
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        JwtBlacklistMock.Verify(
            j => j.RevokeUserBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()),
            Times.Never);
        EmailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
    }
}
