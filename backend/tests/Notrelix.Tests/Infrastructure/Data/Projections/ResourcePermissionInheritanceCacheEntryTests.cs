using Notrelix.Infrastructure.Data.Governance.Projections;

namespace Notrelix.Infrastructure.Tests.Data.Projections;

public class ResourcePermissionInheritanceCacheEntryTests
{
    [Fact]
    public void Create_sets_all_properties()
    {
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var entry = ResourcePermissionInheritanceCacheEntry.Create(
            id, workspaceId, "Board", resourceId,
            null, null, "User", Guid.NewGuid(), null,
            "view", "Allow", "Editor", null, null,
            null, null, 1, "{}", now);

        entry.Id.Should().Be(id);
        entry.WorkspaceId.Should().Be(workspaceId);
        entry.ResourceKind.Should().Be("Board");
        entry.ResourceId.Should().Be(resourceId);
        entry.Action.Should().Be("view");
        entry.Effect.Should().Be("Allow");
        entry.CacheVersion.Should().Be(1);
        entry.ComputedPermissionsJson.Should().Be("{}");
        entry.ComputedAt.Should().Be(now);
    }

    [Fact]
    public void Create_sets_optional_properties()
    {
        var entry = ResourcePermissionInheritanceCacheEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Board", Guid.NewGuid(),
            "Space", Guid.NewGuid(), "User", Guid.NewGuid(), "user-key",
            "edit", "Deny", "Editor", "Direct", Guid.NewGuid(),
            "Workspace", Guid.NewGuid(), 2, "{}", DateTimeOffset.UtcNow);

        entry.ParentResourceType.Should().Be("Space");
        entry.ParentResourceId.Should().NotBeNull();
        entry.SubjectKey.Should().Be("user-key");
        entry.PermissionLevel.Should().Be("Editor");
        entry.SourceType.Should().Be("Direct");
        entry.InheritedFromResourceType.Should().Be("Workspace");
    }
}
