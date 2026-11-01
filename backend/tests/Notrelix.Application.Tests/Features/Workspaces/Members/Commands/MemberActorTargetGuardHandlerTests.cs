using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

/// <summary>
/// WG-DEBT-002 guard 2: Remove, Suspend and Activate are Owner/Admin actions, but a non-Owner
/// actor (Admin) acts only on Member and Guest targets. A rejected request leaves the target
/// and the grant projection untouched.
/// </summary>
public class MemberActorTargetGuardHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Guid _targetId = Guid.CreateVersion7();

    public static TheoryData<WorkspaceRole> ProtectedTargets() => new() { WorkspaceRole.Owner, WorkspaceRole.Admin };

    public static TheoryData<WorkspaceRole> OrdinaryTargets() => new() { WorkspaceRole.Member, WorkspaceRole.Guest };

    private WorkspaceMember Target(WorkspaceRole role, bool suspended = false)
    {
        var member = CreateMember(role, _targetId);
        if (suspended)
        {
            member.Suspend(TestUserId, TestNow, 2);
        }

        return member;
    }

    private void Arrange(WorkspaceRole actorRole, WorkspaceMember target)
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        // A second active Owner keeps the last-owner invariant out of the way: this test is about the guard.
        SetupMembers(CreateMember(actorRole), target, CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));
    }

    private RemoveMemberCommandHandler Remove() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object);

    private SuspendMemberCommandHandler Suspend() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object);

    private ActivateMemberCommandHandler Activate() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object,
        Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g =>
            g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)));

    [Theory]
    [MemberData(nameof(OrdinaryTargets))]
    public async Task Admin_CanRemoveSuspendAndActivate_MemberAndGuestTargets(WorkspaceRole targetRole)
    {
        var target = Target(targetRole);
        Arrange(WorkspaceRole.Admin, target);

        (await Suspend().Handle(new SuspendMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None)).Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Suspended);

        (await Activate().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None)).Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Active);

        (await Remove().Handle(new RemoveMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None)).Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Removed);
    }

    [Fact]
    public async Task Removing_AMember_EndsTheirDerivedAccess_InTheSameRequest()
    {
        var target = Target(WorkspaceRole.Member);
        Arrange(WorkspaceRole.Admin, target);

        await Remove().Handle(new RemoveMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        GrantProjectionMock.Verify(g => g.RevokeAccountGrantWhenLastWorkspaceLeftAsync(
            TestAccountId, TestWorkspaceId, _targetId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        GrantProjectionMock.Verify(g => g.ClearCustomRoleAssignmentsAsync(
            TestAccountId, TestWorkspaceId, _targetId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivating_AMember_ClearsTheirCustomRoleAssignments_ButKeepsTheAccountGrant()
    {
        var target = Target(WorkspaceRole.Member, suspended: true);
        Arrange(WorkspaceRole.Admin, target);

        await Activate().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        GrantProjectionMock.Verify(g => g.ClearCustomRoleAssignmentsAsync(
            TestAccountId, TestWorkspaceId, _targetId, It.IsAny<CancellationToken>()), Times.Once);
        GrantProjectionMock.Verify(g => g.RevokeAccountGrantWhenLastWorkspaceLeftAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ProtectedTargets))]
    public async Task Admin_CannotRemove_OwnerOrAdminTarget(WorkspaceRole targetRole)
    {
        var target = Target(targetRole);
        Arrange(WorkspaceRole.Admin, target);

        Func<Task> act = () => Remove().Handle(new RemoveMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        await AssertRejectedAsync(act, target);
    }

    [Theory]
    [MemberData(nameof(ProtectedTargets))]
    public async Task Admin_CannotSuspend_OwnerOrAdminTarget(WorkspaceRole targetRole)
    {
        var target = Target(targetRole);
        Arrange(WorkspaceRole.Admin, target);

        Func<Task> act = () => Suspend().Handle(new SuspendMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        await AssertRejectedAsync(act, target);
    }

    [Theory]
    [MemberData(nameof(ProtectedTargets))]
    public async Task Admin_CannotActivate_OwnerOrAdminTarget(WorkspaceRole targetRole)
    {
        var target = Target(targetRole, suspended: true);
        Arrange(WorkspaceRole.Admin, target);

        Func<Task> act = () => Activate().Handle(new ActivateMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None);

        await AssertRejectedAsync(act, target, WorkspaceMemberStatus.Suspended);
    }

    [Theory]
    [MemberData(nameof(ProtectedTargets))]
    public async Task Owner_CanStillActOnOwnerAndAdminTargets(WorkspaceRole targetRole)
    {
        var target = Target(targetRole);
        Arrange(WorkspaceRole.Owner, target);

        (await Suspend().Handle(new SuspendMemberCommand(TestWorkspaceId, _targetId, target.Version), CancellationToken.None)).Succeeded.Should().BeTrue();
        target.Status.Should().Be(WorkspaceMemberStatus.Suspended);
    }

    private async Task AssertRejectedAsync(
        Func<Task> act,
        WorkspaceMember target,
        WorkspaceMemberStatus unchangedStatus = WorkspaceMemberStatus.Active)
    {
        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotActOnMember);
        target.Status.Should().Be(unchangedStatus);
        GrantProjectionMock.Verify(
            p => p.RevokeWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
