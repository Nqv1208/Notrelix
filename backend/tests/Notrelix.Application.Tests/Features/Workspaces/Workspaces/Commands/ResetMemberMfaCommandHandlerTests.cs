using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ResetMemberMfa;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class ResetMemberMfaCommandHandlerTests : WorkspaceHandlerTestBase
{
    private const string Proof = "proof";
    private readonly Guid _sessionId = Guid.CreateVersion7();
    private readonly Guid _memberId = Guid.CreateVersion7();
    private readonly Mock<ISecurityStepUpService> _stepUp = new();
    private readonly Mock<IIdentityUserAccess> _access = new();

    /// <summary>The target the proof was given for; the real service refuses any other.</summary>
    private string? _proofGivenFor;

    public ResetMemberMfaCommandHandlerTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(_sessionId);
        _stepUp
            .Setup(s => s.ConsumeBoundAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Invalid());
        _stepUp
            .Setup(s => s.ConsumeBoundAsync(Proof, TestUserId, _sessionId, StepUpPurpose.ResetMemberMfa, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, Guid _, Guid _, StepUpPurpose _, string target, CancellationToken _) =>
                _proofGivenFor is null || _proofGivenFor == target ? Result.Success() : Invalid());
        _access.Setup(a => a.ResetMfaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private static Result Invalid() => Result.Failure(new ApplicationError(
        "identity.security.step-up-invalid", "Invalid.", ApplicationErrorType.Validation));

    private ResetMemberMfaCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, _stepUp.Object, _access.Object);

    private ResetMemberMfaCommand Reset(string proof = Proof) => new(TestWorkspaceId, _memberId, proof);

    private void NothingMayHaveBeenReset() =>
        _access.Verify(a => a.ResetMfaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

    private void SeedOwnerAndMember() =>
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Member, _memberId));

    [Fact]
    public async Task TheOwner_WithAProofForThisMember_ResetsTheMember_AndSpendsTheBoundProofOnce()
    {
        SeedOwnerAndMember();

        var result = await CreateSut().Handle(Reset(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _access.Verify(a => a.ResetMfaAsync(_memberId, TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        _stepUp.Verify(s => s.ConsumeBoundAsync(
            Proof, TestUserId, _sessionId, StepUpPurpose.ResetMemberMfa,
            StepUpTarget.MemberMfaReset(TestWorkspaceId, _memberId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnAdmin_IsRefusedAsNotFound_BeforeAnyProofIsAsked_AndNothingIsReset()
    {
        SetupMembers(CreateMember(WorkspaceRole.Admin), CreateMember(WorkspaceRole.Member, _memberId), CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var act = () => CreateSut().Handle(Reset(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _stepUp.Verify(s => s.ConsumeBoundAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        NothingMayHaveBeenReset();
    }

    [Fact]
    public async Task AStaleOrSpentProof_IsRefusedWith400_AndNothingIsReset()
    {
        SeedOwnerAndMember();

        var result = await CreateSut().Handle(Reset("stale-or-spent"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-invalid" && e.Type == ApplicationErrorType.Validation);
        NothingMayHaveBeenReset();
    }

    [Fact]
    public async Task AProofGivenForAnotherMember_IsRefusedWith400_AndNothingIsReset()
    {
        SeedOwnerAndMember();
        _proofGivenFor = StepUpTarget.MemberMfaReset(TestWorkspaceId, Guid.CreateVersion7());

        var result = await CreateSut().Handle(Reset(), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        NothingMayHaveBeenReset();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithoutAProof_IsRefusedWith412_AndNothingIsReset(string token)
    {
        SeedOwnerAndMember();

        var result = await CreateSut().Handle(Reset(token), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        NothingMayHaveBeenReset();
    }

    [Fact]
    public async Task AnOwnersFactors_AreRefused_EvenWithAValidProof()
    {
        // The target owns another workspace in the same scope, so no Owner is reachable.
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Owner, _memberId));

        var act = () => CreateSut().Handle(Reset(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        NothingMayHaveBeenReset();
    }

    [Fact]
    public async Task TheCallersOwnFactors_AreNotResetThroughThisPath()
    {
        SetupMembers(CreateMember(WorkspaceRole.Owner));

        var act = () => CreateSut().Handle(new ResetMemberMfaCommand(TestWorkspaceId, TestUserId, Proof), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        NothingMayHaveBeenReset();
    }

    [Fact]
    public async Task AnAnotherWorkspacesMember_IsNotFound()
    {
        SetupMembers(CreateMember(WorkspaceRole.Owner));

        var act = () => CreateSut().Handle(Reset(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        NothingMayHaveBeenReset();
    }
}
