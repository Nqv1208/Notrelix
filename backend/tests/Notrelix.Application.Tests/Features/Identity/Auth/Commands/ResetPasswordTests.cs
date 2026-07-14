using Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ResetPasswordTests : IdentityHandlerTestBase
{
    private ResetPasswordCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        OtpServiceMock.Object,
        PasswordHasherMock.Object,
        EmailServiceMock.Object,
        DateTimeProviderMock.Object,
        ResetPasswordLoggerMock.Object);

    [Fact]
    public async Task Handle_WhenValidOtp_HashesPasswordAndRevokesSessions()
    {
        var user = CreateUser();
        var session = CreateSession();
        SetupUsers(user);
        SetupSessions(session);

        OtpServiceMock.Setup(o => o.GetAttemptsAsync("forgot-password", TestEmail.ToLowerInvariant()))
            .ReturnsAsync(0);
        OtpServiceMock.Setup(o => o.ValidateAsync("forgot-password", TestEmail.ToLowerInvariant(), "valid-code"))
            .ReturnsAsync(true);
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);

        var sut = CreateSut();
        var result = await sut.Handle(new ResetPasswordCommand
        {
            Email = TestEmail,
            Code = "valid-code",
            NewPassword = TestPassword
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
        EmailServiceMock.Verify(e => e.SendAsync(TestEmail.ToLowerInvariant(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvalidOtp_ReturnsFailure()
    {
        OtpServiceMock.Setup(o => o.GetAttemptsAsync("forgot-password", TestEmail.ToLowerInvariant()))
            .ReturnsAsync(0);
        OtpServiceMock.Setup(o => o.ValidateAsync("forgot-password", TestEmail.ToLowerInvariant(), "bad-code"))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.Handle(new ResetPasswordCommand
        {
            Email = TestEmail,
            Code = "bad-code",
            NewPassword = TestPassword
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid or expired code"));
    }

    [Fact]
    public async Task Handle_WhenTooManyAttempts_ReturnsFailure()
    {
        OtpServiceMock.Setup(o => o.GetAttemptsAsync("forgot-password", TestEmail.ToLowerInvariant()))
            .ReturnsAsync(5);

        var sut = CreateSut();
        var result = await sut.Handle(new ResetPasswordCommand
        {
            Email = TestEmail,
            Code = "any-code",
            NewPassword = TestPassword
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Too many failed attempts"));
    }
}
