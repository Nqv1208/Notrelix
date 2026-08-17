using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ChangePasswordTests : IdentityHandlerTestBase
{
    private const string ValidProof = "verified-proof-token";

    private ChangePasswordCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        PasswordHasherMock.Object,
        JwtBlacklistMock.Object,
        EmailServiceMock.Object,
        DateTimeProviderMock.Object,
        ChangePasswordLoggerMock.Object,
        StepUpServiceMock.Object);

    private void SetupActiveMfa()
    {
        var method = UserMfaMethod.Create(
            TestUserId, MfaMethodType.AuthenticatorApp, TestNow, secretRef: SecretRef.Create("protected-secret"));
        method.Verify(TestNow);
        SetupUserMfaMethods(method);
    }

    private void SetupValidProof()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(ValidProof, TestUserId, TestSessionId, StepUpPurpose.ChangePassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

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

    [Fact]
    public async Task Handle_WhenMfaActiveAndStepUpTokenMissing_ReturnsStepUpRequiredWithoutMutation()
    {
        var user = CreateUser();
        SetupUsers(user);
        SetupActiveMfa();
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Strong verification is required"));
        user.PasswordHash.Should().Be(TestHashedPassword);
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        JwtBlacklistMock.Verify(
            j => j.RevokeUserBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMfaActiveAndProofInvalid_ReturnsFailureWithoutMutation()
    {
        var user = CreateUser();
        SetupUsers(user);
        SetupActiveMfa();
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("stale-or-forged-token", TestUserId, TestSessionId, StepUpPurpose.ChangePassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed)));

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!",
            StepUpToken = "stale-or-forged-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        user.PasswordHash.Should().Be(TestHashedPassword);
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        JwtBlacklistMock.Verify(
            j => j.RevokeUserBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMfaActiveAndProofValid_SucceedsAndConsumesChangePasswordProof()
    {
        var user = CreateUser();
        var session = CreateSession();
        SetupUsers(user);
        SetupSessions(session);
        SetupActiveMfa();
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        PasswordHasherMock.Setup(h => h.HashPassword("NewPassword123!")).Returns("new-hashed-password");
        SetupValidProof();

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!",
            StepUpToken = ValidProof
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hashed-password");
        session.Status.Should().Be(SessionStatus.Revoked);
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(ValidProof, TestUserId, TestSessionId, StepUpPurpose.ChangePassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_DoesNotConsumeOtherwiseValidProof()
    {
        var user = CreateUser();
        SetupUsers(user);
        SetupActiveMfa();
        PasswordHasherMock.Setup(h => h.VerifyPassword("wrong-password", TestHashedPassword)).Returns(false);
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = "wrong-password",
            NewPassword = "NewPassword123!",
            StepUpToken = ValidProof
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Current password is incorrect"));
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
        user.PasswordHash.Should().Be(TestHashedPassword);
    }

    [Fact]
    public async Task Handle_WhenNoMfa_IgnoresStepUpToken()
    {
        var user = CreateUser();
        SetupUsers(user);
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);
        PasswordHasherMock.Setup(h => h.HashPassword("NewPassword123!")).Returns("new-hashed-password");

        var sut = CreateSut();
        var result = await sut.Handle(new ChangePasswordCommand
        {
            CurrentPassword = TestPassword,
            NewPassword = "NewPassword123!",
            StepUpToken = "any-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hashed-password");
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}