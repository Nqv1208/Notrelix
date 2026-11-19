using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class TransferOwnershipCommandHandlerTests : WorkspaceHandlerTestBase
{
    private const string Proof = "proof";
    private readonly Guid _sessionId = Guid.CreateVersion7();
    private readonly Mock<ISecurityStepUpService> _stepUp = new();

    public TransferOwnershipCommandHandlerTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(_sessionId);
        // Only the proof of this caller, this session and this purpose spends; anything else is refused like the real service.
        _stepUp
            .Setup(s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.security.step-up-invalid", "Invalid.", ApplicationErrorType.Validation)));
        _stepUp
            .Setup(s => s.ConsumeAsync(Proof, TestUserId, _sessionId, StepUpPurpose.TransferWorkspaceOwnership, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private TransferOwnershipCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object, _stepUp.Object);

    private void SeedOwnerAndAdmin(out Guid adminId)
    {
        adminId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Admin, adminId));
    }

    private void OwnershipMustNotHaveChanged(Guid adminId)
    {
        var rows = DbContextMock.Object.WorkspaceMembers.ToList();
        rows.Single(m => m.UserId == TestUserId).Role.Should().Be(WorkspaceRole.Owner);
        rows.Single(m => m.UserId == adminId).Role.Should().Be(WorkspaceRole.Admin);
        GrantProjectionMock.Verify(g => g.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithoutAProof_IsRefusedWith412_AndOwnershipDoesNotChange(string token)
    {
        SeedOwnerAndAdmin(out var adminId);

        var result = await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, adminId, 1, token), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        OwnershipMustNotHaveChanged(adminId);
    }

    [Fact]
    public async Task Handle_WithASessionlessCaller_IsRefusedWith412_AndOwnershipDoesNotChange()
    {
        SeedOwnerAndAdmin(out var adminId);
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var result = await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, adminId, 1, Proof), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-required");
        OwnershipMustNotHaveChanged(adminId);
    }

    [Theory]
    [InlineData("stale-or-spent")]
    [InlineData("proof-of-another-purpose")]
    public async Task Handle_WithAStaleSpentOrOtherPurposeProof_IsRefusedWith400_AndOwnershipDoesNotChange(string token)
    {
        SeedOwnerAndAdmin(out var adminId);

        var result = await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, adminId, 1, token), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-invalid" && e.Type == ApplicationErrorType.Validation);
        OwnershipMustNotHaveChanged(adminId);
    }

    [Fact]
    public async Task Handle_SpendsTheProofBoundToThisCallerSessionAndPurpose_Once()
    {
        SeedOwnerAndAdmin(out var adminId);

        var result = await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, adminId, 1, Proof), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _stepUp.Verify(s => s.ConsumeAsync(
            Proof, TestUserId, _sessionId, StepUpPurpose.TransferWorkspaceOwnership, It.IsAny<CancellationToken>()), Times.Once);
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == adminId).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task Handle_WhenTheCallerIsNotTheOwner_IsNotFoundBeforeAnyProofIsAsked()
    {
        var targetId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Admin), CreateMember(WorkspaceRole.Member, targetId), CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, targetId, 1, ""), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _stepUp.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWorkspaceAndMembersExist_TransfersOwnership()
    {
        var newOwnerId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(
            CreateMember(WorkspaceRole.Owner),
            CreateMember(WorkspaceRole.Member, newOwnerId));
        var sut = CreateSut();
        var result = await sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, newOwnerId, 1, Proof), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1, Proof), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNewOwnerNotMember_ThrowsNotFoundException()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1, Proof), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PromotesTheTarget_DemotesTheCaller_ToAdmin()
    {
        var newOwnerId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Admin, newOwnerId));

        await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, newOwnerId, 1, Proof), CancellationToken.None);

        var rows = DbContextMock.Object.WorkspaceMembers.ToList();
        rows.Single(m => m.UserId == newOwnerId).Role.Should().Be(WorkspaceRole.Owner);
        rows.Single(m => m.UserId == TestUserId).Role.Should().Be(WorkspaceRole.Admin);
        GrantProjectionMock.Verify(g => g.SyncWorkspaceMemberGrantAsync(
            TestAccountId, TestWorkspaceId, newOwnerId, WorkspaceRole.Owner, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheTargetIsAGuest_IsRefused_AndNobodyChanges()
    {
        var guestId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Guest, guestId));

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, guestId, 1, Proof), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_CannotPromoteGuestToOwner);
        var rows = DbContextMock.Object.WorkspaceMembers.ToList();
        rows.Single(m => m.UserId == TestUserId).Role.Should().Be(WorkspaceRole.Owner);
        rows.Single(m => m.UserId == guestId).Role.Should().Be(WorkspaceRole.Guest);
        GrantProjectionMock.Verify(g => g.SyncWorkspaceMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheTargetIsSuspended_IsNotFound_AndNobodyChanges()
    {
        var targetId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var target = CreateMember(WorkspaceRole.Admin, targetId);
        target.Suspend(TestUserId, TestNow, 2);
        SetupMembers(CreateMember(WorkspaceRole.Owner), target);

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, targetId, 1, Proof), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == TestUserId).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task Handle_ToYourself_IsRefused()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner));

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, TestUserId, 1, Proof), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.BusinessRuleException>();
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Handle_WhenTheCallerIsNotAnOwner_IsNotFound_AndNobodyIsPromoted(WorkspaceRole callerRole)
    {
        var targetId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(callerRole), CreateMember(WorkspaceRole.Member, targetId), CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, targetId, 1, Proof), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == targetId).Role.Should().Be(WorkspaceRole.Member);
    }
}
