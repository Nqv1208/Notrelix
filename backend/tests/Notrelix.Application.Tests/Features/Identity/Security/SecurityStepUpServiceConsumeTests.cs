using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>A verified proof opens one action of one purpose in one session of one person, once.</summary>
public class SecurityStepUpServiceConsumeTests : IdentityHandlerTestBase
{
    private const string Token = "proof-token";
    private readonly InMemoryStepUpProofStore _proofs;

    public SecurityStepUpServiceConsumeTests()
    {
        _proofs = new InMemoryStepUpProofStore(DateTimeProviderMock.Object);
    }

    private SecurityStepUpService CreateSut() => new(
        IdentityContextMock.Object,
        ChallengeStoreMock.Object,
        _proofs,
        new Mock<Notrelix.Application.Features.Identity.Mfa.Abstractions.IMfaCodeVerifier>().Object,
        PasswordHasherMock.Object,
        DateTimeProviderMock.Object,
        ThrottleMock.Object);

    private Task Issue(Guid? userId = null, Guid? sessionId = null, StepUpPurpose purpose = StepUpPurpose.EnrollMfa, TimeSpan? ttl = null, string? target = null) =>
        _proofs.StoreAsync(
            Token,
            new StepUpProofPayload(userId ?? TestUserId, sessionId ?? TestSessionId, purpose, TestNow, TestNow.Add(ttl ?? TimeSpan.FromMinutes(5)), target),
            TimeSpan.FromMinutes(5));

    [Fact]
    public async Task AMatchingProof_IsAccepted()
    {
        await Issue();

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AProofBoundToAnotherSession_IsRefused()
    {
        await Issue(sessionId: Guid.NewGuid());

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
    }

    [Fact]
    public async Task AProofBoundToAnotherUser_IsRefused()
    {
        await Issue(userId: Guid.NewGuid());

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AProofOfAnotherPurpose_IsRefused()
    {
        await Issue(purpose: StepUpPurpose.DisableMfa);

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AReplayedProof_IsRefused()
    {
        await Issue();
        var sut = CreateSut();

        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeTrue();
        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AMismatchedConsume_BurnsTheProof()
    {
        await Issue();
        var sut = CreateSut();

        (await sut.ConsumeAsync(Token, TestUserId, Guid.NewGuid(), StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
        (await sut.ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AnExpiredProof_IsRefused()
    {
        await Issue(ttl: TimeSpan.FromSeconds(-1));

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    private static readonly Guid Workspace = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid PersonX = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid PersonY = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly string TargetX = StepUpTarget.OwnershipTransfer(Workspace, PersonX);
    private static readonly string TargetY = StepUpTarget.OwnershipTransfer(Workspace, PersonY);

    private Task<Result> ConsumeBound(string target) =>
        CreateSut().ConsumeBoundAsync(Token, TestUserId, TestSessionId, StepUpPurpose.TransferWorkspaceOwnership, target, CancellationToken.None);

    [Fact]
    public async Task AProofGivenForATarget_IsAcceptedForThatTarget()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership, target: TargetX);

        (await ConsumeBound(TargetX)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AProofGivenForOnePerson_IsRefusedForAnother()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership, target: TargetX);

        var result = await ConsumeBound(TargetY);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
    }

    [Fact]
    public async Task AProofGivenForOneWorkspace_IsRefusedForAnother()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership, target: TargetX);
        var otherWorkspace = StepUpTarget.OwnershipTransfer(Guid.NewGuid(), PersonX);

        (await ConsumeBound(otherWorkspace)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AProofGivenWithNoTarget_IsRefusedWhereATargetIsRequired()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership);

        (await ConsumeBound(TargetX)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AProofGivenForATarget_IsRefusedWhereNoTargetIsChecked()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership, target: TargetX);

        var result = await CreateSut().ConsumeAsync(Token, TestUserId, TestSessionId, StepUpPurpose.TransferWorkspaceOwnership, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ARefusedTarget_BurnsTheProof_SoItCannotBeTriedAgainstTheRightOne()
    {
        await Issue(purpose: StepUpPurpose.TransferWorkspaceOwnership, target: TargetX);

        (await ConsumeBound(TargetY)).Succeeded.Should().BeFalse();
        (await ConsumeBound(TargetX)).Succeeded.Should().BeFalse();
    }
}
