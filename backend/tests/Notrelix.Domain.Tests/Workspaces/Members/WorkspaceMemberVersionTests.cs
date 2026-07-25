using FluentAssertions;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceMemberVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

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

    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.SoftDelete(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.IsDeleted.Should().BeTrue();
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRemovedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.Restore(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.IsDeleted.Should().BeFalse();
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRestoredDomainEvent);
    }
}
