using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceMemberVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(WorkspaceMember), nameof(WorkspaceMember.ChangeRole), MutationScenario.Version, typeof(WorkspaceRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.ChangeRole(WorkspaceRole.Admin, _actorId, 2, _now);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRoleChangedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), nameof(WorkspaceMember.Suspend), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.Suspend(_actorId, _now, 2);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberSuspendedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), nameof(WorkspaceMember.Activate), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.Suspend(_actorId, _now, 2);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.Activate(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberActivatedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), nameof(WorkspaceMember.Remove), MutationScenario.Lifecycle, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.Remove(2, _actorId, _now);

        member.Version.Should().Be(version + 1);
        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRemovedDomainEvent);
    }
}
