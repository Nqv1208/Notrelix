using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Tests.Governance.Permissions;

public class ResourcePermissionLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ResourcePermission_Delete_ShouldRaiseEvent()
    {
        var permission = ResourcePermission.Grant(Guid.NewGuid(), WsA, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        ((IHasDomainEvents)permission).ClearDomainEvents();
        var version = permission.Version;

        permission.Delete(Actor, Now);

        permission.IsDeleted.Should().BeTrue();
        permission.Version.Should().Be(version + 1);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionDeletedDomainEvent);
        var evt = (ResourcePermissionDeletedDomainEvent)permission.DomainEvents.Single(e => e is ResourcePermissionDeletedDomainEvent);
        evt.PermissionId.Should().Be(permission.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void ResourcePermission_Restore_ShouldRaiseEvent()
    {
        var permission = ResourcePermission.Grant(Guid.NewGuid(), WsA, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.Delete(Actor, Now);
        ((IHasDomainEvents)permission).ClearDomainEvents();
        var version = permission.Version;

        permission.Restore(Actor, Now);

        permission.IsDeleted.Should().BeFalse();
        permission.Version.Should().Be(version + 1);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionRestoredDomainEvent);
        var evt = (ResourcePermissionRestoredDomainEvent)permission.DomainEvents.Single(e => e is ResourcePermissionRestoredDomainEvent);
        evt.PermissionId.Should().Be(permission.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void ResourcePermission_Delete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var permission = ResourcePermission.Grant(Guid.NewGuid(), WsA, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        permission.Delete(Actor, Now);
        ((IHasDomainEvents)permission).ClearDomainEvents();
        var version = permission.Version;

        permission.Delete(Actor, Now);

        permission.Version.Should().Be(version);
        permission.DomainEvents.Should().NotContain(e => e is ResourcePermissionDeletedDomainEvent);
    }

    [Fact]
    public void ResourcePermission_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var permission = ResourcePermission.Grant(Guid.NewGuid(), WsA, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        ((IHasDomainEvents)permission).ClearDomainEvents();
        var version = permission.Version;

        permission.Restore(Actor, Now);

        permission.Version.Should().Be(version);
        permission.DomainEvents.Should().NotContain(e => e is ResourcePermissionRestoredDomainEvent);
    }

    [Fact]
    public void ResourcePermission_Revoke_ShouldEmitOnlyRevokedEvent()
    {
        var permission = ResourcePermission.Grant(Guid.NewGuid(), WsA, ResourceKind.Create("work-management.board"), Guid.NewGuid(), PermissionSubjectType.User, Actor, PermissionLevel.Editor, Actor, Now);
        ((IHasDomainEvents)permission).ClearDomainEvents();
        var version = permission.Version;

        permission.Revoke(Actor, Now);

        permission.IsDeleted.Should().BeTrue();
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionRevokedDomainEvent);
        permission.DomainEvents.Should().NotContain(e => e is ResourcePermissionDeletedDomainEvent);
    }
}
