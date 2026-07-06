using FluentAssertions;
using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Domain.Tests.Collaboration;

public class ResourceWatcherWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => ResourceWatcher.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.WorkspaceId.Should().Be(WsA);
    }
}
