using FluentAssertions;
using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Domain.Tests.Collaboration;

public class ResourceWatcherTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId);

        var watcher = ResourceWatcher.Create(workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        watcher.WorkspaceId.Should().Be(workspaceId);
        watcher.Target.Should().Be(target);
        watcher.Level.Should().Be(WatchLevel.All);
        watcher.DomainEvents.Should().ContainSingle(e => e is ResourceWatchedDomainEvent);
    }

    [Fact]
    public void Create_WithSpecificLevel_ShouldSetLevel()
    {
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, WatchLevel.MentionsOnly);

        watcher.Level.Should().Be(WatchLevel.MentionsOnly);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), Guid.NewGuid());

        var act = () => ResourceWatcher.Create(workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Unwatch_ShouldRaiseEvent_AndSetDeleted()
    {
        var watcher = CreateWatcher();
        watcher.ClearDomainEvents();

        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);

        watcher.IsDeleted.Should().BeTrue();
        watcher.DomainEvents.Should().ContainSingle(e => e is ResourceUnwatchedDomainEvent);
    }

    [Fact]
    public void Unwatch_WhenAlreadyDeleted_ShouldThrow()
    {
        var watcher = CreateWatcher();
        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    private static ResourceWatcher CreateWatcher()
    {
        var workspaceId = Guid.NewGuid();
        return ResourceWatcher.Create(workspaceId, ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
