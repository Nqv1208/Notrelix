using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class TransferOwnershipCommandHandlerTests : WorkspaceHandlerTestBase
{
    private TransferOwnershipCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceAndMembersExist_TransfersOwnership()
    {
        var newOwnerId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(
            CreateMember(WorkspaceRole.Owner),
            CreateMember(WorkspaceRole.Member, newOwnerId));
        var sut = CreateSut();
        var result = await sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, newOwnerId, 1), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNewOwnerNotMember_ThrowsNotFoundException()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PromotesTheTarget_DemotesTheCaller_ToAdmin()
    {
        var newOwnerId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner), CreateMember(WorkspaceRole.Admin, newOwnerId));

        await CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, newOwnerId, 1), CancellationToken.None);

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

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, guestId, 1), CancellationToken.None);

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

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, targetId, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == TestUserId).Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public async Task Handle_ToYourself_IsRefused()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner));

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, TestUserId, 1), CancellationToken.None);

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

        var act = () => CreateSut().Handle(new TransferOwnershipCommand(TestWorkspaceId, targetId, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == targetId).Role.Should().Be(WorkspaceRole.Member);
    }
}
