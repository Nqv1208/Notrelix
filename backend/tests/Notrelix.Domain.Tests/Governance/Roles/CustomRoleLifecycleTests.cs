using FluentAssertions;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Domain.Tests.Governance.Roles;

public class CustomRoleLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CustomRole_Archive_ShouldRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Archive(Actor, Now);

        role.Status.Should().Be(CustomRoleStatus.Archived);
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleArchivedDomainEvent);
        var evt = (CustomRoleArchivedDomainEvent)role.DomainEvents.Single(e => e is CustomRoleArchivedDomainEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.ArchivedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Archive_WhenAlreadyArchived_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.Archive(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Archive(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleArchivedDomainEvent);
    }

    [Fact]
    public void CustomRole_Activate_ShouldRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.Archive(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Activate(Actor, Now);

        role.Status.Should().Be(CustomRoleStatus.Active);
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleActivatedDomainEvent);
        var evt = (CustomRoleActivatedDomainEvent)role.DomainEvents.Single(e => e is CustomRoleActivatedDomainEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.ActivatedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Activate_WhenNotArchived_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Activate(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleActivatedDomainEvent);
    }

    [Fact]
    public void CustomRole_SoftDelete_ShouldRaiseDedicatedEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.SoftDelete(Actor, Now);

        role.IsDeleted.Should().BeTrue();
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleSoftDeletedDomainEvent);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleUpdatedDomainEvent);
        var evt = (CustomRoleSoftDeletedDomainEvent)role.DomainEvents.Single(e => e is CustomRoleSoftDeletedDomainEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_Restore_ShouldRaiseDedicatedEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.SoftDelete(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Restore(Actor, Now);

        role.IsDeleted.Should().BeFalse();
        role.Version.Should().Be(version + 1);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleRestoredDomainEvent);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleUpdatedDomainEvent);
        var evt = (CustomRoleRestoredDomainEvent)role.DomainEvents.Single(e => e is CustomRoleRestoredDomainEvent);
        evt.RoleId.Should().Be(role.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void CustomRole_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.SoftDelete(Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.SoftDelete(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleSoftDeletedDomainEvent);
    }

    [Fact]
    public void CustomRole_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var role = CustomRole.Create(WsA, "Admin", null, Actor, Now);
        role.ClearDomainEvents();
        var version = role.Version;

        role.Restore(Actor, Now);

        role.Version.Should().Be(version);
        role.DomainEvents.Should().NotContain(e => e is CustomRoleRestoredDomainEvent);
    }
}
