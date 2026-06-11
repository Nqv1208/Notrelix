using FluentAssertions;
using Notrelix.Domain.Governance.Events;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Domain.Tests;

public class GovernanceDomainTests
{
    [Fact]
    public void ResourcePermission_Create_ShouldRaiseResourcePermissionGrantedEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        // Act
        var permission = ResourcePermission.Create(
            workspaceId,
            ResourceType.Board,
            resourceId,
            SubjectType.User,
            userId,
            PermissionLevel.Editor,
            grantedBy);

        // Assert
        permission.WorkspaceId.Should().Be(workspaceId);
        permission.ResourceType.Should().Be(ResourceType.Board);
        permission.ResourceId.Should().Be(resourceId);
        permission.SubjectType.Should().Be(SubjectType.User);
        permission.SubjectId.Should().Be(userId);
        permission.Level.Should().Be(PermissionLevel.Editor);
        permission.GrantedBy.Should().Be(grantedBy);
        permission.IsRevoked.Should().BeFalse();
        permission.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        permission.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ResourcePermissionGrantedEvent>();

        var evt = (ResourcePermissionGrantedEvent)permission.DomainEvents.Single();
        evt.PermissionId.Should().Be(permission.Id);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.ResourceType.Should().Be(ResourceType.Board);
        evt.ResourceId.Should().Be(resourceId);
        evt.SubjectType.Should().Be(SubjectType.User);
        evt.SubjectId.Should().Be(userId);
        evt.Level.Should().Be(PermissionLevel.Editor);
        evt.GrantedBy.Should().Be(grantedBy);
    }

    [Fact]
    public void ResourcePermission_UpdateLevel_ShouldUpdateLevelAndRaiseEvent()
    {
        // Arrange
        var permission = ResourcePermission.Create(
            Guid.NewGuid(),
            ResourceType.Board,
            Guid.NewGuid(),
            SubjectType.User,
            Guid.NewGuid(),
            PermissionLevel.Viewer,
            Guid.NewGuid());
        permission.ClearDomainEvents();
        var updatedBy = Guid.NewGuid();

        // Act
        permission.UpdateLevel(PermissionLevel.Owner, updatedBy);

        // Assert
        permission.Level.Should().Be(PermissionLevel.Owner);
        permission.UpdatedBy.Should().Be(updatedBy);
        permission.UpdatedAt.Should().NotBeNull();

        permission.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ResourcePermissionUpdatedEvent>();

        var evt = (ResourcePermissionUpdatedEvent)permission.DomainEvents.Single();
        evt.PermissionId.Should().Be(permission.Id);
        evt.WorkspaceId.Should().Be(permission.WorkspaceId);
        evt.NewLevel.Should().Be(PermissionLevel.Owner);
        evt.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void ResourcePermission_Revoke_ShouldSetRevokedAndRaiseEvent()
    {
        // Arrange
        var permission = ResourcePermission.Create(
            Guid.NewGuid(),
            ResourceType.Board,
            Guid.NewGuid(),
            SubjectType.User,
            Guid.NewGuid(),
            PermissionLevel.Viewer,
            Guid.NewGuid());
        permission.ClearDomainEvents();
        var revokedBy = Guid.NewGuid();

        // Act
        permission.Revoke(revokedBy);

        // Assert
        permission.IsRevoked.Should().BeTrue();
        permission.RevokedBy.Should().Be(revokedBy);
        permission.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        permission.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ResourcePermissionRevokedEvent>();

        var evt = (ResourcePermissionRevokedEvent)permission.DomainEvents.Single();
        evt.PermissionId.Should().Be(permission.Id);
        evt.WorkspaceId.Should().Be(permission.WorkspaceId);
        evt.RevokedBy.Should().Be(revokedBy);
    }

    [Fact]
    public void CustomRole_Create_ShouldRaiseCustomRoleCreatedEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var role = CustomRole.Create(
            workspaceId,
            "Custom Manager",
            createdBy,
            "Description of Custom Manager",
            "#FF0000",
            false,
            true);

        // Assert
        role.WorkspaceId.Should().Be(workspaceId);
        role.Name.Should().Be("Custom Manager");
        role.Description.Should().Be("Description of Custom Manager");
        role.Color.Should().Be("#FF0000");
        role.IsSystem.Should().BeFalse();
        role.IsAssignable.Should().BeTrue();
        role.CreatedByUserId.Should().Be(createdBy);

        role.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomRoleCreatedEvent>();

        var evt = (CustomRoleCreatedEvent)role.DomainEvents.Single();
        evt.RoleId.Should().Be(role.Id);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.Name.Should().Be("Custom Manager");
        evt.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void CustomRole_Rename_ShouldRenameAndRaiseEvent()
    {
        // Arrange
        var role = CustomRole.Create(Guid.NewGuid(), "Old Name", Guid.NewGuid());
        role.ClearDomainEvents();
        var updatedBy = Guid.NewGuid();

        // Act
        role.Rename("New Name", updatedBy);

        // Assert
        role.Name.Should().Be("New Name");
        role.UpdatedBy.Should().Be(updatedBy);

        role.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomRoleUpdatedEvent>();

        var evt = (CustomRoleUpdatedEvent)role.DomainEvents.Single();
        evt.RoleId.Should().Be(role.Id);
        evt.WorkspaceId.Should().Be(role.WorkspaceId);
        evt.Name.Should().Be("New Name");
        evt.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void CustomRole_SoftDelete_ShouldSetDeletedAndRaiseEvent()
    {
        // Arrange
        var role = CustomRole.Create(Guid.NewGuid(), "Role to Delete", Guid.NewGuid());
        role.ClearDomainEvents();
        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTime.UtcNow;

        // Act
        role.SoftDelete(deletedBy, deletedAt, "No longer needed");

        // Assert
        role.IsDeleted.Should().BeTrue();
        role.DeletedAt.Should().Be(deletedAt);
        role.DeletedBy.Should().Be(deletedBy);
        role.DeleteReason.Should().Be("No longer needed");

        role.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomRoleDeletedEvent>();

        var evt = (CustomRoleDeletedEvent)role.DomainEvents.Single();
        evt.RoleId.Should().Be(role.Id);
        evt.WorkspaceId.Should().Be(role.WorkspaceId);
        evt.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void ShareLink_Create_ShouldRaiseShareLinkCreatedEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var shareLink = ShareLink.Create(
            workspaceId,
            ResourceType.Page,
            resourceId,
            "abc123token",
            PermissionLevel.Viewer,
            null,
            createdBy);

        // Assert
        shareLink.WorkspaceId.Should().Be(workspaceId);
        shareLink.ResourceType.Should().Be(ResourceType.Page);
        shareLink.ResourceId.Should().Be(resourceId);
        shareLink.TokenHash.Should().Be("abc123token");
        shareLink.Level.Should().Be(PermissionLevel.Viewer);
        shareLink.IsEnabled.Should().BeTrue();
        shareLink.ExpiresAt.Should().BeNull();
        shareLink.CreatedBy.Should().Be(createdBy);

        shareLink.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShareLinkCreatedEvent>();

        var evt = (ShareLinkCreatedEvent)shareLink.DomainEvents.Single();
        evt.ShareLinkId.Should().Be(shareLink.Id);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.ResourceType.Should().Be(ResourceType.Page);
        evt.ResourceId.Should().Be(resourceId);
        evt.Level.Should().Be(PermissionLevel.Viewer);
        evt.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void ShareLink_Disable_ShouldDisableAndRaiseEvent()
    {
        // Arrange
        var shareLink = ShareLink.Create(
            Guid.NewGuid(),
            ResourceType.Page,
            Guid.NewGuid(),
            "tokenhash",
            PermissionLevel.Viewer,
            null,
            Guid.NewGuid());
        shareLink.ClearDomainEvents();
        var disabledBy = Guid.NewGuid();

        // Act
        shareLink.Disable(disabledBy);

        // Assert
        shareLink.IsEnabled.Should().BeFalse();
        shareLink.DisabledBy.Should().Be(disabledBy);
        shareLink.DisabledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        shareLink.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShareLinkDisabledEvent>();

        var evt = (ShareLinkDisabledEvent)shareLink.DomainEvents.Single();
        evt.ShareLinkId.Should().Be(shareLink.Id);
        evt.WorkspaceId.Should().Be(shareLink.WorkspaceId);
        evt.DisabledBy.Should().Be(disabledBy);
    }
}
