using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Domain.Workspaces;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class AddMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IBillingSeatGate> _seatGate = new();
    private readonly Mock<IAccountMembershipFacts> _accountFacts = new();

    public AddMemberCommandHandlerTests()
    {
        _seatGate.Setup(g => g.CanAdmitAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _accountFacts.Setup(f => f.IsActiveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private AddMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, _seatGate.Object, _accountFacts.Object);

    [Theory]
    [InlineData("another account")]
    [InlineData("inactive account member")]
    [InlineData("unknown user")]
    public async Task Handle_WhenTargetIsNotAnActiveAccountMember_IsRefusedIdenticallyAndNothingChanges(string _)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var userId = Guid.CreateVersion7();
        _accountFacts.Setup(f => f.IsActiveMemberAsync(workspace.AccountId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Member), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which;
        ex.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_NotInAccount);
        _seatGate.Verify(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        DbContextMock.Verify(c => c.WorkspaceMembers.Add(It.IsAny<WorkspaceMember>()), Times.Never);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member, false)]
    [InlineData(WorkspaceRole.Guest, true)]
    public async Task Handle_WhenNoSeatIsAvailable_IsRefusedWithStableCodeAndNothingChanges(WorkspaceRole role, bool asGuest)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var userId = Guid.CreateVersion7();
        _seatGate.Setup(g => g.CanAdmitAsync(workspace.AccountId, workspace.Id, userId, asGuest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, role), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminAddsGuestAndMember_AsksForAGuestSeatAndAMemberSeat()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Admin));

        await CreateSut().Handle(new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), WorkspaceRole.Guest), CancellationToken.None);
        await CreateSut().Handle(new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), WorkspaceRole.Member), CancellationToken.None);

        _seatGate.Verify(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _seatGate.Verify(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkspaceExists_AddsMemberSuccessfully()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), WorkspaceRole.Member);
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new AddMemberCommand(TestWorkspaceId, Guid.CreateVersion7(), WorkspaceRole.Member);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMemberAdded_SyncsWorkspaceGrantForNewMember()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var sut = CreateSut();
        var newUserId = Guid.CreateVersion7();
        var command = new AddMemberCommand(workspace.Id, newUserId, WorkspaceRole.Member);

        await sut.Handle(command, CancellationToken.None);

        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                workspace.AccountId, workspace.Id, newUserId, WorkspaceRole.Member, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReactivatingExistingMember_SyncsWorkspaceGrantWithRequestedRole()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var returningUserId = Guid.CreateVersion7();
        var suspendedMember = CreateMember(WorkspaceRole.Member, returningUserId);
        suspendedMember.Suspend(TestUserId, TestNow, 2);
        SetupMembers(CreateMember(WorkspaceRole.Owner), suspendedMember);
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, returningUserId, WorkspaceRole.Admin);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                workspace.AccountId, workspace.Id, returningUserId, WorkspaceRole.Admin, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Handle_WhenAdminAddsMemberOrGuest_Succeeds(WorkspaceRole assigned)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Admin));

        var result = await CreateSut().Handle(
            new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), assigned), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task Handle_WhenAdminAddsOwnerOrAdmin_IsRejectedWithStableCodeAndNothingChanges(WorkspaceRole assigned)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Admin));

        Func<Task> act = () => CreateSut().Handle(
            new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), assigned), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminReAddsASuspendedOwnerRow_IsRejected()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var formerOwnerId = Guid.CreateVersion7();
        var formerOwner = CreateMember(WorkspaceRole.Owner, formerOwnerId);
        formerOwner.Suspend(TestUserId, TestNow, 2);
        SetupMembers(CreateMember(WorkspaceRole.Admin), formerOwner);

        Func<Task> act = () => CreateSut().Handle(
            new AddMemberCommand(workspace.Id, formerOwnerId, WorkspaceRole.Member), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotActOnMember);
    }

    [Fact]
    public async Task Handle_WhenTheActorHoldsNoActiveMembership_FailsClosed()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers();

        Func<Task> act = () => CreateSut().Handle(
            new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), WorkspaceRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
