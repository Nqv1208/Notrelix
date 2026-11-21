using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.SetFirstPassword;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class SetFirstPasswordTests : IdentityHandlerTestBase
{
    private const string Proof = "verified-proof-token";

    private SetFirstPasswordCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        PasswordHasherMock.Object,
        JwtBlacklistMock.Object,
        EmailServiceMock.Object,
        DateTimeProviderMock.Object,
        StepUpServiceMock.Object,
        ActivityMock.Object,
        new Mock<ILogger<SetFirstPasswordCommandHandler>>().Object);

    private User CreateProviderOnlyUser()
    {
        var user = User.Create(TestEmail, "Test User", "sentinel-hash", TestNow, hasPasswordCredential: false);
        user.GetType().GetProperty(nameof(User.Id))!.SetValue(user, TestUserId);
        return user;
    }

    private void SetupProof(Result? answer = null)
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(Proof, TestUserId, TestSessionId, StepUpPurpose.ChangePassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer ?? Result.Success());
    }

    [Fact]
    public async Task Handle_WithProof_SetsPasswordEndsOtherDevicesKeepsThisOneAndNotifies()
    {
        var user = CreateProviderOnlyUser();
        var thisDevice = CreateSession();
        thisDevice.GetType().GetProperty(nameof(UserSession.Id))!.SetValue(thisDevice, TestSessionId);
        var otherDevice = CreateSession();
        SetupUsers(user);
        SetupSessions(thisDevice, otherDevice);
        SetupProof();
        PasswordHasherMock.Setup(h => h.HashPassword("NewPassword123!")).Returns("new-hash");

        var result = await CreateSut().Handle(new SetFirstPasswordCommand("NewPassword123!", Proof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.HasPasswordCredential.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        otherDevice.Status.Should().Be(SessionStatus.Revoked);
        thisDevice.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(j => j.RevokeSessionBeforeAsync(otherDevice.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        JwtBlacklistMock.Verify(j => j.RevokeUserBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        EmailServiceMock.Verify(e => e.SendAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutProof_ChangesNothing()
    {
        var user = CreateProviderOnlyUser();
        SetupUsers(user);
        SetupSessions();
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);

        var result = await CreateSut().Handle(new SetFirstPasswordCommand("NewPassword123!"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        user.HasPasswordCredential.Should().BeFalse();
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProofIsRefused_ChangesNothing()
    {
        var user = CreateProviderOnlyUser();
        SetupUsers(user);
        SetupSessions();
        SetupProof(Result.Failure(new ApplicationError("identity.security.step-up-invalid", "No.", ApplicationErrorType.PreconditionFailed)));

        var result = await CreateSut().Handle(new SetFirstPasswordCommand("NewPassword123!", Proof), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        user.HasPasswordCredential.Should().BeFalse();
        PasswordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAPasswordExists_IsConflictAndSpendsNoProof()
    {
        SetupUsers(CreateUser());
        SetupSessions();
        SetupProof();

        var result = await CreateSut().Handle(new SetFirstPasswordCommand("NewPassword123!", Proof), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.auth.password-already-set" && e.Type == ApplicationErrorType.Conflict);
        StepUpServiceMock.Verify(
            s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Validator_RefusesAShortPassword()
    {
        new SetFirstPasswordCommandValidator().Validate(new SetFirstPasswordCommand("short", Proof)).IsValid.Should().BeFalse();
        new SetFirstPasswordCommandValidator().Validate(new SetFirstPasswordCommand("LongEnoughPassword1!", Proof)).IsValid.Should().BeTrue();
    }
}
