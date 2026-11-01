using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class RegenerateRecoveryCodesTests : IdentityHandlerTestBase
{
    private readonly Mock<IMfaRecoveryCodeGenerator> _recovery = new();

    public RegenerateRecoveryCodesTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        _recovery.Setup(r => r.Generate(MfaPolicy.RecoveryCodeCount))
            .Returns(Enumerable.Range(0, MfaPolicy.RecoveryCodeCount).Select(i => $"new-{i}").ToArray());
        _recovery.Setup(r => r.Hash(It.IsAny<string>())).Returns((string c) => $"hash-{c}");
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("proof", TestUserId, TestSessionId, StepUpPurpose.RegenerateRecoveryCodes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private RegenerateRecoveryCodesCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        _recovery.Object,
        StepUpServiceMock.Object,
        DateTimeProviderMock.Object,
        new Mock<ILogger<RegenerateRecoveryCodesCommandHandler>>().Object);

    private UserMfaMethod ActiveMethod(Guid? userId = null)
    {
        var id = userId ?? TestUserId;
        var method = UserMfaMethod.Create(id, MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("protected"));
        method.GetType().GetProperty(nameof(UserMfaMethod.UserId))!.SetValue(method, id);
        method.Verify(TestNow);
        return method;
    }

    private MfaRecoveryBatch Batch(Guid? userId = null) =>
        MfaRecoveryBatch.Create(userId ?? TestUserId, Enumerable.Range(0, MfaPolicy.RecoveryCodeCount).Select(i => $"old-{i}"), TestNow, userId ?? TestUserId);

    private static RegenerateRecoveryCodesCommand Ask(string token = "proof") => new() { StepUpToken = token };

    [Fact]
    public async Task WithAProof_VoidsTheOldBatchAndIssuesOneNewBatchShownOnce()
    {
        SetupUserMfaMethods(ActiveMethod());
        var old = Batch();
        var batches = new List<MfaRecoveryBatch> { old };
        IdentityContextMock.Setup(c => c.MfaRecoveryBatches).Returns(CreateAsyncDbSet(batches));

        var result = await CreateSut().Handle(Ask(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RecoveryCodes.Should().HaveCount(MfaPolicy.RecoveryCodeCount).And.OnlyContain(c => c.StartsWith("new-"));
        old.InvalidatedAt.Should().Be(TestNow);
        batches.Should().HaveCount(2);
        batches.Count(b => b.InvalidatedAt is null).Should().Be(1);
        batches.Single(b => b.InvalidatedAt is null).Id.Should().Be(result.Data!.MfaMethodId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task WithoutAProof_IsRefused_AndTheOldCodesStayValid(string token)
    {
        SetupUserMfaMethods(ActiveMethod());
        var old = Batch();
        SetupMfaRecoveryBatches(old);

        var result = await CreateSut().Handle(Ask(token), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        old.InvalidatedAt.Should().BeNull();
        _recovery.Verify(r => r.Generate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AWrongOrSpentProof_IsRefused_AndTheOldCodesStayValid()
    {
        SetupUserMfaMethods(ActiveMethod());
        var old = Batch();
        SetupMfaRecoveryBatches(old);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("spent", TestUserId, TestSessionId, StepUpPurpose.RegenerateRecoveryCodes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError("identity.security.step-up-invalid", "Invalid proof.", ApplicationErrorType.Validation)));

        var result = await CreateSut().Handle(Ask("spent"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        old.InvalidatedAt.Should().BeNull();
        _recovery.Verify(r => r.Generate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task WithNoActiveFactor_IsRefused_BeforeAnyProofIsSpent()
    {
        SetupMfaRecoveryBatches();

        var result = await CreateSut().Handle(Ask(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.mfa.not-enabled" && e.Type == ApplicationErrorType.PreconditionFailed);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithNoSessionBound_IsRefusedAsStepUpRequired()
    {
        SetupUserMfaMethods(ActiveMethod());
        var old = Batch();
        SetupMfaRecoveryBatches(old);
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var result = await CreateSut().Handle(Ask(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-required");
        old.InvalidatedAt.Should().BeNull();
    }

    [Fact]
    public async Task AnotherPersonsFactorAndBatch_AreNeverUsedOrVoided()
    {
        var theirsUser = Guid.CreateVersion7();
        SetupUserMfaMethods(ActiveMethod(theirsUser));
        var theirs = Batch(theirsUser);
        SetupMfaRecoveryBatches(theirs);

        var result = await CreateSut().Handle(Ask(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.mfa.not-enabled");
        theirs.InvalidatedAt.Should().BeNull();
    }
}
