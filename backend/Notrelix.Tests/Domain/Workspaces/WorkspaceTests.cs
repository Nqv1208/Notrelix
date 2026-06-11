using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Rules;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Name.Should().Be("My Workspace");
        workspace.Slug.Should().Be("my-workspace");
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceCreatedEvent);
    }

    [Fact]
    public void CreateMember_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        
        var member = WorkspaceMember.Create(workspaceId, userId, WorkspaceRole.Member, addedBy, DateTimeOffset.UtcNow);

        member.WorkspaceId.Should().Be(workspaceId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(WorkspaceRole.Member);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberAddedEvent);
    }

    [Fact]
    public void ChangeMemberRole_ShouldChangeRole_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.ClearDomainEvents();

        member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Role.Should().Be(WorkspaceRole.Admin);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRoleChangedEvent);
    }

    [Fact]
    public void RemoveMember_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.ClearDomainEvents();

        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.IsDeleted.Should().BeTrue();
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRemovedEvent);
    }

    [Fact]
    public void CreateWithOwner_ShouldCreateWorkspaceAndOwnerMember()
    {
        var ownerId = Guid.NewGuid();
        var result = WorkspaceFactory.CreateWithOwner(ownerId, "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        result.Workspace.Should().NotBeNull();
        result.Workspace.Name.Should().Be("My Workspace");
        result.Workspace.Slug.Should().Be("my-workspace");

        result.OwnerMember.Should().NotBeNull();
        result.OwnerMember.WorkspaceId.Should().Be(result.Workspace.Id);
        result.OwnerMember.UserId.Should().Be(ownerId);
        result.OwnerMember.Role.Should().Be(WorkspaceRole.Owner);
        result.OwnerMember.Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    [Fact]
    public void ChangeRole_OnDeletedMember_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Suspend_OnDeletedMember_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_OnDeletedMember_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SoftDelete_ShouldSetStatusToRemoved()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
        member.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Activate_FromSuspended_ShouldSetStatusToActive()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Status.Should().Be(WorkspaceMemberStatus.Suspended);

        member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    [Fact]
    public void Activate_FromRemoved_ShouldThrow()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted and cannot be modified*");
    }

    [Fact]
    public void WorkspaceMemberEvents_ShouldContainUserId()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Added event
        var member = WorkspaceMember.Create(workspaceId, userId, WorkspaceRole.Member, actorId, now);
        var addedEvent = (WorkspaceMemberAddedEvent)member.DomainEvents.First(e => e is WorkspaceMemberAddedEvent);
        addedEvent.UserId.Should().Be(userId);
        addedEvent.WorkspaceId.Should().Be(workspaceId);
        addedEvent.ActorId.Should().Be(actorId);

        // Role changed event
        member.ClearDomainEvents();
        member.ChangeRole(WorkspaceRole.Admin, actorId, now);
        var roleChangedEvent = (WorkspaceMemberRoleChangedEvent)member.DomainEvents.First(e => e is WorkspaceMemberRoleChangedEvent);
        roleChangedEvent.UserId.Should().Be(userId);
        roleChangedEvent.WorkspaceId.Should().Be(workspaceId);
        roleChangedEvent.ActorId.Should().Be(actorId);

        // Suspended event
        member.ClearDomainEvents();
        member.Suspend(actorId, now);
        var suspendedEvent = (WorkspaceMemberSuspendedEvent)member.DomainEvents.First(e => e is WorkspaceMemberSuspendedEvent);
        suspendedEvent.UserId.Should().Be(userId);
        suspendedEvent.WorkspaceId.Should().Be(workspaceId);
        suspendedEvent.ActorId.Should().Be(actorId);

        // Activated event
        member.ClearDomainEvents();
        member.Activate(actorId, now);
        var activatedEvent = (WorkspaceMemberActivatedEvent)member.DomainEvents.First(e => e is WorkspaceMemberActivatedEvent);
        activatedEvent.UserId.Should().Be(userId);
        activatedEvent.WorkspaceId.Should().Be(workspaceId);
        activatedEvent.ActorId.Should().Be(actorId);

        // Removed event
        member.ClearDomainEvents();
        member.SoftDelete(actorId, now);
        var removedEvent = (WorkspaceMemberRemovedEvent)member.DomainEvents.First(e => e is WorkspaceMemberRemovedEvent);
        removedEvent.UserId.Should().Be(userId);
        removedEvent.WorkspaceId.Should().Be(workspaceId);
        removedEvent.ActorId.Should().Be(actorId);
    }

    [Fact]
    public void WorkspaceOwnerRules_ShouldNotAllowActionsOnLastOwner()
    {
        // Downgrade
        var actDowngrade = () => WorkspaceOwnerRules.EnsureCanDowngradeOwner(WorkspaceRole.Owner, WorkspaceRole.Admin, 1);
        actDowngrade.Should().Throw<BusinessRuleException>().WithMessage("*Cannot downgrade the last owner*");

        // Suspend
        var actSuspend = () => WorkspaceOwnerRules.EnsureCanSuspendOwner(WorkspaceRole.Owner, 1);
        actSuspend.Should().Throw<BusinessRuleException>().WithMessage("*Cannot suspend the last owner*");

        // Remove
        var actRemove = () => WorkspaceOwnerRules.EnsureCanRemoveOwner(WorkspaceRole.Owner, 1);
        actRemove.Should().Throw<BusinessRuleException>().WithMessage("*Cannot remove the last owner*");
    }

    [Fact]
    public void WorkspaceOwnerRules_ShouldAllowActionsIfMultipleOwners()
    {
        // Downgrade
        var actDowngrade = () => WorkspaceOwnerRules.EnsureCanDowngradeOwner(WorkspaceRole.Owner, WorkspaceRole.Admin, 2);
        actDowngrade.Should().NotThrow();

        // Suspend
        var actSuspend = () => WorkspaceOwnerRules.EnsureCanSuspendOwner(WorkspaceRole.Owner, 2);
        actSuspend.Should().NotThrow();

        // Remove
        var actRemove = () => WorkspaceOwnerRules.EnsureCanRemoveOwner(WorkspaceRole.Owner, 2);
        actRemove.Should().NotThrow();
    }
}
