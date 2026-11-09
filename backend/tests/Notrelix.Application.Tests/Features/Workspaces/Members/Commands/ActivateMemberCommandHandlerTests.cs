using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

/// <summary>
/// UC-WSP-F04: a suspended member takes a seat again only when Billing admits them; an active member is a no-op
/// that asks no seat and takes no version step; a removed member is refused.
/// </summary>
public class ActivateMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IBillingSeatGate> _seatGate = new();
    private readonly Guid _targetId = Guid.CreateVersion7();

    public ActivateMemberCommandHandlerTests()
    {
        _seatGate.Setup(g => g.CanAdmitAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private ActivateMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, _seatGate.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);

    private WorkspaceMember Suspended(WorkspaceRole role = WorkspaceRole.Member)
    {
        var member = CreateMember(role, _targetId);
        member.Suspend(TestUserId, TestNow, 2);
        return member;
    }

    private void Arrange(WorkspaceMember target)
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        // A second active Owner keeps the last-owner invariant out of the way; the actor is the Owner.
        SetupMembers(CreateMember(WorkspaceRole.Owner), target);
    }

    [Fact]
    public async Task Activate_ASuspendedMember_WhenNoSeatIsFree_IsRefusedWithStableCode_AndStaysSuspended()
    {
        var target = Suspended();
        Arrange(target);
        _seatGate.Setup(g => g.CanAdmitAsync(TestAccountId, TestWorkspaceId, _targetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        target.Status.Should().Be(WorkspaceMemberStatus.Suspended);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Activate_ASuspendedGuest_AsksForAGuestSeat()
    {
        var target = Suspended(WorkspaceRole.Guest);
        Arrange(target);
        _seatGate.Setup(g => g.CanAdmitAsync(TestAccountId, TestWorkspaceId, _targetId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => CreateSut().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        _seatGate.Verify(g => g.CanAdmitAsync(TestAccountId, TestWorkspaceId, _targetId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_ASuspendedMember_WhenASeatIsFree_ActivatesAndSyncsTheGrant()
    {
        var target = Suspended();
        Arrange(target);
        var versionBefore = target.Version;

        var result = await CreateSut().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, versionBefore), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Active);
        target.Version.Should().Be(versionBefore + 1);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(TestAccountId, TestWorkspaceId, _targetId, WorkspaceRole.Member, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Activate_AnActiveMember_IsANoOp_AsksNoSeat_AndTakesNoVersionStep()
    {
        var target = CreateMember(WorkspaceRole.Member, _targetId);
        Arrange(target);
        var versionBefore = target.Version;

        var result = await CreateSut().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, versionBefore), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Active);
        target.Version.Should().Be(versionBefore);
        _seatGate.Verify(
            g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Activate_ARemovedMember_IsRefused_AndAsksNoSeat()
    {
        var target = CreateMember(WorkspaceRole.Member, _targetId);
        target.Remove(2, TestUserId, TestNow);
        Arrange(target);

        Func<Task> act = () => CreateSut().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved);
        target.Status.Should().Be(WorkspaceMemberStatus.Removed);
        _seatGate.Verify(
            g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
