using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class StartMfaEnrollmentTests : IdentityHandlerTestBase
{
    private readonly Mock<IMfaTotpService> _totp = new();

    public StartMfaEnrollmentTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        SetupUsers(CreateUser());
        _totp.Setup(t => t.GenerateSecretKey()).Returns("SECRET");
        _totp.Setup(t => t.ProtectSecret("SECRET")).Returns("protected");
        _totp.Setup(t => t.BuildOtpAuthUri("SECRET", It.IsAny<string>(), It.IsAny<string>())).Returns("otpauth://x");
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("proof", TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private StartMfaEnrollmentCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        _totp.Object,
        StepUpServiceMock.Object,
        DateTimeProviderMock.Object,
        new Mock<ILogger<StartMfaEnrollmentCommandHandler>>().Object);

    private UserMfaMethod Pending()
    {
        var method = UserMfaMethod.Create(TestUserId, MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("old"));
        method.GetType().GetProperty(nameof(UserMfaMethod.UserId))!.SetValue(method, TestUserId);
        return method;
    }

    private static StartMfaEnrollmentCommand Start(string proof = "proof") => new() { StepUpToken = proof };

    [Fact]
    public async Task WithoutAProof_IsRefusedWith412_AndCreatesNothing()
    {
        var methods = new List<UserMfaMethod>();
        IdentityContextMock.Setup(c => c.UserMfaMethods).Returns(CreateAsyncDbSet(methods));

        var result = await CreateSut().Handle(Start(""), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        methods.Should().BeEmpty();
    }

    [Fact]
    public async Task WithASessionlessCaller_IsRefusedWith412()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var result = await CreateSut().Handle(Start(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-required");
    }

    [Fact]
    public async Task AWrongSpentOrOtherPurposeProof_IsRefused_AndCreatesNothing()
    {
        var methods = new List<UserMfaMethod>();
        IdentityContextMock.Setup(c => c.UserMfaMethods).Returns(CreateAsyncDbSet(methods));
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("bad", TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.security.step-up-invalid", "Invalid.", ApplicationErrorType.Validation)));

        var result = await CreateSut().Handle(Start("bad"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-invalid" && e.Type == ApplicationErrorType.Validation);
        methods.Should().BeEmpty();
    }

    [Fact]
    public async Task WithAnActiveFactor_Answers409First_AndKeepsTheProof()
    {
        var active = Pending();
        active.Verify(TestNow);
        SetupUserMfaMethods(active);

        var result = await CreateSut().Handle(Start(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.mfa.already-enabled" && e.Type == ApplicationErrorType.Conflict);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithAProof_CreatesOnePendingMethod_AndShowsTheSecretOnlyInTheAnswer()
    {
        var methods = new List<UserMfaMethod>();
        IdentityContextMock.Setup(c => c.UserMfaMethods).Returns(CreateAsyncDbSet(methods));

        var result = await CreateSut().Handle(Start(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Secret.Should().Be("SECRET");
        var created = methods.Should().ContainSingle().Subject;
        created.Status.Should().Be(MfaMethodStatus.PendingVerification);
        created.UserId.Should().Be(TestUserId);
        created.SecretRef!.Value.Should().Be("protected");
    }

    [Fact]
    public async Task StartingAgain_ReplacesThePendingMethod()
    {
        var old = Pending();
        var methods = new List<UserMfaMethod> { old };
        IdentityContextMock.Setup(c => c.UserMfaMethods).Returns(CreateAsyncDbSet(methods));

        var result = await CreateSut().Handle(Start(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        old.Status.Should().Be(MfaMethodStatus.Disabled);
        methods.Where(m => m.Status == MfaMethodStatus.PendingVerification).Should().ContainSingle();
    }

    [Fact]
    public async Task AnotherPersonsPendingMethod_IsNotTouched()
    {
        var other = UserMfaMethod.Create(Guid.CreateVersion7(), MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("theirs"));
        var methods = new List<UserMfaMethod> { other };
        IdentityContextMock.Setup(c => c.UserMfaMethods).Returns(CreateAsyncDbSet(methods));

        await CreateSut().Handle(Start(), CancellationToken.None);

        other.Status.Should().Be(MfaMethodStatus.PendingVerification);
    }
}
