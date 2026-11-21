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

    private void VerifyNoGrantSync() =>
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);

    private WorkspaceMember RowOf(Guid userId) =>
        DbContextMock.Object.WorkspaceMembers.AsEnumerable().Single(m => m.UserId == userId);

    private WorkspaceMember ReturningMember(Guid userId, WorkspaceMemberStatus status)
    {
        var member = CreateMember(WorkspaceRole.Member, userId);
        if (status == WorkspaceMemberStatus.Suspended) member.Suspend(TestUserId, TestNow, 2);
        if (status == WorkspaceMemberStatus.Removed) member.Remove(2, TestUserId, TestNow);
        return member;
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Handle_WhenTheTargetIsNotInTheAccount_EvenWithARemovedRowOrAGuestRole_IsRefusedAndTheRowStaysAsItWas(WorkspaceRole role)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var removed = ReturningMember(userId, WorkspaceMemberStatus.Removed);
        var addedBefore = removed.DomainEvents.OfType<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>().Count();
        SetupMembers(CreateMember(WorkspaceRole.Owner), removed);
        _accountFacts.Setup(f => f.IsActiveMemberAsync(workspace.AccountId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, role), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_NotInAccount);
        removed.Status.Should().Be(WorkspaceMemberStatus.Removed);
        removed.DomainEvents.OfType<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>().Should().HaveCount(addedBefore, "no member-added fact is raised");
        VerifyNoGrantSync();
    }

    [Fact]
    public async Task Handle_WhenAnOwnerAddsAnExistingAccountMember_CreatesAnActiveRow_RaisesMemberAdded_AndSyncsTheGrant()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var userId = Guid.CreateVersion7();

        var result = await CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Admin), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var row = RowOf(userId);
        row.Status.Should().Be(WorkspaceMemberStatus.Active);
        row.Role.Should().Be(WorkspaceRole.Admin);
        row.DomainEvents.OfType<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>().Should().ContainSingle()
            .Which.Should().Match<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>(
                e => e.UserId == userId && e.Role == WorkspaceRole.Admin && e.ActorId == TestUserId);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(workspace.AccountId, workspace.Id, userId, WorkspaceRole.Admin, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheUserIsAlreadyActive_IsRefusedAndNothingChanges()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var active = CreateMember(WorkspaceRole.Member, userId);
        SetupMembers(CreateMember(WorkspaceRole.Owner), active);
        var version = active.Version;

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Guest), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.BusinessRuleException>();
        active.Role.Should().Be(WorkspaceRole.Member, "an add never changes an active member's role");
        active.Version.Should().Be(version);
        VerifyNoGrantSync();
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task Handle_WhenARemovedMemberReturns_TheSameRowIsActiveAgainWithTheNewRole_AndMemberAddedIsRaised(WorkspaceRole role)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var removed = ReturningMember(userId, WorkspaceMemberStatus.Removed);
        var addedBefore = removed.DomainEvents.OfType<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>().Count();
        SetupMembers(CreateMember(WorkspaceRole.Owner), removed);

        var result = await CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, role), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        removed.Status.Should().Be(WorkspaceMemberStatus.Active);
        removed.Role.Should().Be(role);
        removed.DomainEvents.OfType<Notrelix.Domain.Workspaces.Members.Events.WorkspaceMemberAddedDomainEvent>().Should().HaveCount(addedBefore + 1)
            .And.Subject.Last().Role.Should().Be(role);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(workspace.AccountId, workspace.Id, userId, role, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
        GrantProjectionMock.Verify(
            p => p.ClearCustomRoleAssignmentsAsync(workspace.AccountId, workspace.Id, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenASuspendedMemberIsAddedAgain_TheyAreActiveWithTheNewRole()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var suspended = ReturningMember(userId, WorkspaceMemberStatus.Suspended);
        SetupMembers(CreateMember(WorkspaceRole.Owner), suspended);

        var result = await CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Guest), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        suspended.Status.Should().Be(WorkspaceMemberStatus.Active);
        suspended.Role.Should().Be(WorkspaceRole.Guest);
    }

    [Theory]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Member, false)]
    [InlineData(WorkspaceMemberStatus.Removed, WorkspaceRole.Guest, true)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Member, false)]
    [InlineData(WorkspaceMemberStatus.Suspended, WorkspaceRole.Guest, true)]
    public async Task Handle_WhenAReturningMemberNeedsASeatAndNoneIsFree_IsRefusedAndTheRowStaysAsItWas(
        WorkspaceMemberStatus status, WorkspaceRole role, bool asGuest)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var returning = ReturningMember(userId, status);
        SetupMembers(CreateMember(WorkspaceRole.Owner), returning);
        _seatGate.Setup(g => g.CanAdmitAsync(workspace.AccountId, workspace.Id, userId, asGuest, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, role), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        returning.Status.Should().Be(status);
        returning.Role.Should().Be(WorkspaceRole.Member);
        VerifyNoGrantSync();
    }

    [Theory]
    [InlineData(WorkspaceMemberStatus.Active)]
    [InlineData(WorkspaceMemberStatus.Removed)]
    public async Task Handle_WhenAnOwnerAddsAnOwner_IsRefusedWithTheOwnershipCode_AndNothingChanges(WorkspaceMemberStatus existing)
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId);
        SetupWorkspaces(workspace);
        var userId = Guid.CreateVersion7();
        var row = ReturningMember(userId, existing);
        SetupMembers(CreateMember(WorkspaceRole.Owner), row);
        var asNew = Guid.CreateVersion7();

        Func<Task> forNewUser = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, asNew, WorkspaceRole.Owner), CancellationToken.None);
        Func<Task> forExisting = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Owner), CancellationToken.None);

        (await forNewUser.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotDirectlyAssignOwner);
        await forExisting.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        DbContextMock.Object.WorkspaceMembers.AsEnumerable().Should().NotContain(m => m.UserId == asNew);
        row.Role.Should().Be(WorkspaceRole.Member);
        VerifyNoGrantSync();
    }

    [Fact]
    public async Task Handle_WhenTheWorkspaceIsArchived_IsNotFoundAndNothingChanges()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId, isArchived: true);
        SetupWorkspaces(workspace);
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var userId = Guid.CreateVersion7();

        Func<Task> act = () => CreateSut().Handle(new AddMemberCommand(workspace.Id, userId, WorkspaceRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.AsEnumerable().Should().NotContain(m => m.UserId == userId);
        VerifyNoGrantSync();
    }
}
