using Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Domain.Identity.Security;
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
        JwtBlacklistMock.Object,
        ThrottleMock.Object,
        ActivityMock.Object,
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

    [Fact]
    public async Task Handle_WhenResetSucceeds_ClearsAccountLockoutAndRevokesIssuedAccessTokens()
    {
        var user = CreateUser();
        SetupUsers(user);
        SetupSessions(CreateSession());
        SetupValidOtp("valid-code");

        var result = await CreateSut().Handle(
            new ResetPasswordCommand { Email = TestEmail, Code = "valid-code", NewPassword = TestPassword },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        ThrottleMock.Verify(t => t.ClearSubjectAsync(
            LoginThrottleScope.PasswordLogin, TestEmail.ToLowerInvariant(), It.IsAny<CancellationToken>()), Times.Once);
        ThrottleMock.Verify(t => t.ClearSubjectAsync(
            It.Is<LoginThrottleScope>(s => s != LoginThrottleScope.PasswordLogin),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        // The per-IP counter is never touched by a reset.
        ThrottleMock.Verify(t => t.RecordSuccessAsync(
            It.IsAny<LoginThrottleScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        JwtBlacklistMock.Verify(b => b.RevokeUserBeforeAsync(user.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        ActivityMock.Verify(a => a.Record(user.Id, SecurityActivityType.PasswordReset, TestNow, null), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCodeInvalid_DoesNotClearLockoutOrRevokeTokens()
    {
        OtpServiceMock.Setup(o => o.GetAttemptsAsync("forgot-password", TestEmail.ToLowerInvariant())).ReturnsAsync(0);
        OtpServiceMock.Setup(o => o.ValidateAsync("forgot-password", TestEmail.ToLowerInvariant(), "bad"))
            .ReturnsAsync(false);

        var result = await CreateSut().Handle(
            new ResetPasswordCommand { Email = TestEmail, Code = "bad", NewPassword = TestPassword },
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        ThrottleMock.Verify(t => t.ClearSubjectAsync(
            It.IsAny<LoginThrottleScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        JwtBlacklistMock.Verify(b => b.RevokeUserBeforeAsync(
            It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.VerifyNoOtherCalls();
    }

    private void SetupValidOtp(string code)
    {
        OtpServiceMock.Setup(o => o.GetAttemptsAsync("forgot-password", TestEmail.ToLowerInvariant())).ReturnsAsync(0);
        OtpServiceMock.Setup(o => o.ValidateAsync("forgot-password", TestEmail.ToLowerInvariant(), code)).ReturnsAsync(true);
        PasswordHasherMock.Setup(h => h.HashPassword(TestPassword)).Returns(TestHashedPassword);
    }
}
