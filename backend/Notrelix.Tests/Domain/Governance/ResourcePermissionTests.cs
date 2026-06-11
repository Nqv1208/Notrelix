using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Permissions;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class ResourcePermissionTests
{
    [Fact]
    public void Grant_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        var permission = ResourcePermission.Grant(
            workspaceId,
            ResourceType.Board, 
            resourceId, 
            PermissionSubjectType.User, 
            subjectId, 
            PermissionLevel.Editor, 
            grantedBy,
            DateTimeOffset.UtcNow);

        permission.ResourceType.Should().Be(ResourceType.Board);
        permission.Level.Should().Be(PermissionLevel.Editor);
        permission.CreatedBy.Should().Be(grantedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionGrantedEvent);
    }

    [Fact]
    public void ChangeLevel_ShouldUpdateLevel_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var permission = ResourcePermission.Grant(
            workspaceId, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        permission.ClearDomainEvents();
        
        var updatedBy = Guid.NewGuid();
        permission.ChangeLevel(PermissionLevel.Editor, updatedBy, DateTimeOffset.UtcNow);

        permission.Level.Should().Be(PermissionLevel.Editor);
        permission.UpdatedBy.Should().Be(updatedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionLevelChangedEvent);
    }

    [Fact]
    public void Revoke_ShouldSoftDelete_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var permission = ResourcePermission.Grant(
            workspaceId, ResourceType.Workspace, Guid.NewGuid(), PermissionSubjectType.Team, Guid.NewGuid(), PermissionLevel.Viewer, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        permission.ClearDomainEvents();
        
        var revokedBy = Guid.NewGuid();
        permission.Revoke(revokedBy, DateTimeOffset.UtcNow);

        permission.IsDeleted.Should().BeTrue();
        permission.DeletedBy.Should().Be(revokedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionRevokedEvent);
    }
}
