using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Tests.Governance;

[CoversAggregate(typeof(ResourcePermission))]
public class ResourcePermissionTests
{
    [Fact]
    public void Grant_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        var permission = ResourcePermission.Grant(Guid.NewGuid(),
            workspaceId,
            ResourceType.Board,
            resourceId,
            PermissionSubjectType.User,
            subjectId,
            PermissionLevel.Editor,
            PermissionLevel.Owner,
            grantedBy,
            DateTimeOffset.UtcNow);

        permission.ResourceType.Should().Be(ResourceType.Board);
        permission.Level.Should().Be(PermissionLevel.Editor);
        permission.CreatedBy.Should().Be(grantedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionGrantedDomainEvent);
    }

    [CoversMutation(typeof(ResourcePermission), "ChangeLevel(Notrelix.Domain.Governance.Permissions.PermissionLevel,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void ChangeLevel_ShouldUpdateLevel_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var permission = ResourcePermission.Grant(Guid.NewGuid(),
            workspaceId, ResourceType.Board, Guid.NewGuid(), PermissionSubjectType.User, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        ((IHasDomainEvents)permission).ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        permission.ChangeLevel(PermissionLevel.Editor, updatedBy, DateTimeOffset.UtcNow);

        permission.Level.Should().Be(PermissionLevel.Editor);
        permission.UpdatedBy.Should().Be(updatedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionLevelChangedDomainEvent);
    }

    [CoversMutation(typeof(ResourcePermission), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Revoke_ShouldSoftDelete_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var permission = ResourcePermission.Grant(Guid.NewGuid(),
            workspaceId, ResourceType.Workspace, Guid.NewGuid(), PermissionSubjectType.Team, Guid.NewGuid(), PermissionLevel.Viewer, PermissionLevel.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        ((IHasDomainEvents)permission).ClearDomainEvents();

        var revokedBy = Guid.NewGuid();
        permission.Revoke(revokedBy, DateTimeOffset.UtcNow);

        permission.IsDeleted.Should().BeTrue();
        permission.DeletedBy.Should().Be(revokedBy);
        permission.DomainEvents.Should().ContainSingle(e => e is ResourcePermissionRevokedDomainEvent);
    }
}
