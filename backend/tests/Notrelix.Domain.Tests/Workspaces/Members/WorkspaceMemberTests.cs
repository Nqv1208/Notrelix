using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

[CoversAggregate(typeof(WorkspaceMember))]
public class WorkspaceMemberTests
{
    [Fact]
    public void CreateMember_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var member = WorkspaceMember.Create(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Member, addedBy, now);

        member.WorkspaceId.Should().Be(workspaceId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(WorkspaceRole.Member);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberAddedDomainEvent);

        var evt = (WorkspaceMemberAddedDomainEvent)member.DomainEvents.First();
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.UserId.Should().Be(userId);
        evt.Role.Should().Be(WorkspaceRole.Member);
        evt.ActorId.Should().Be(addedBy);
        evt.OccurredAt.Should().Be(now);
    }

    [CoversMutation(typeof(WorkspaceMember), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.Int32,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void ChangeMemberRole_ShouldChangeRole_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.ChangeRole(WorkspaceRole.Admin, actor, 2, now);

        member.Role.Should().Be(WorkspaceRole.Admin);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRoleChangedDomainEvent);
        var evt = (WorkspaceMemberRoleChangedDomainEvent)member.DomainEvents.First();
        evt.NewRole.Should().Be(WorkspaceRole.Admin);
        evt.ActorId.Should().Be(actor);
        evt.OccurredAt.Should().Be(now);
    }

    [CoversMutation(typeof(WorkspaceMember), "PromoteToOwner(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ChangeMemberRole_OnLastOwner_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), 1, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the last owner of the workspace.");
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Event)]
    [Fact]
    public void Suspend_ShouldSetStatusToSuspended_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Suspend(actor, now, 2);

        member.Status.Should().Be(WorkspaceMemberStatus.Suspended);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberSuspendedDomainEvent);
        var evt = (WorkspaceMemberSuspendedDomainEvent)member.DomainEvents.First();
        evt.ActorId.Should().Be(actor);
        evt.OccurredAt.Should().Be(now);
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Invalid)]
    [CoversMutation(typeof(WorkspaceMember), "PromoteToOwner(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Suspend_OnLastOwner_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 1);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot suspend the last owner of the workspace.");
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Event)]
    [Fact]
    public void Activate_FromSuspended_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Activate(actor, now);

        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberActivatedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), "Remove(System.Int32,System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Invalid)]
    [Fact]
    public void Activate_FromRemoved_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted and cannot be modified*");
    }

    [CoversMutation(typeof(WorkspaceMember), "Remove(System.Int32,System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void RemoveMember_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Remove(2, actor, now);

        member.IsDeleted.Should().BeTrue();
        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRemovedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), "Remove(System.Int32,System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Invalid)]
    [CoversMutation(typeof(WorkspaceMember), "PromoteToOwner(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RemoveMember_OnLastOwner_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Remove(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last owner of the workspace.");
    }

    [CoversMutation(typeof(WorkspaceMember), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Restore(actor, now);

        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.IsDeleted.Should().BeFalse();
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRestoredDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceMember), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.Int32,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ChangeRole_OnDeletedMember_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Invalid)]
    [Fact]
    public void Suspend_OnDeletedMember_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        act.Should().Throw<DomainException>();
    }

    [CoversMutation(typeof(WorkspaceMember), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.Int32,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(WorkspaceMember), "PromoteToOwner(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ChangeRole_LastOwner_ShouldNotMutateRole()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var originalRole = member.Role;

        var act = () => member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), 1, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        member.Role.Should().Be(originalRole);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Valid)]
    [CoversMutation(typeof(WorkspaceMember), "PromoteToOwner(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Suspend_LastOwner_ShouldNotMutateStatus()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var originalStatus = member.Status;

        var act = () => member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 1);

        act.Should().Throw<BusinessRuleException>();
        member.Status.Should().Be(originalStatus);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceMember), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.Int32,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ChangeRole_EmptyActor_ShouldNotMutateRole()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var originalRole = member.Role;

        var act = () => member.ChangeRole(WorkspaceRole.Admin, Guid.Empty, 2, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        member.Role.Should().Be(originalRole);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceMember), "Suspend(System.Guid,System.DateTimeOffset,System.Int32)", MutationScenario.Valid)]
    [Fact]
    public void Suspend_EmptyActor_ShouldNotMutateStatus()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var originalStatus = member.Status;

        var act = () => member.Suspend(Guid.Empty, DateTimeOffset.UtcNow, 2);

        act.Should().Throw<BusinessRuleException>();
        member.Status.Should().Be(originalStatus);
        member.DomainEvents.Should().BeEmpty();
    }
}
