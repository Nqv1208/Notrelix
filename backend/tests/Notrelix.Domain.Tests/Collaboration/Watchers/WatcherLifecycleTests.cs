using FluentAssertions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Collaboration.Watchers;

public class WatcherLifecycleTests
{
    [Fact]
    public void Create_ShouldSetDefaultWatchLevel()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId);
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.Level.Should().Be(WatchLevel.All);
    }

    [Fact]
    public void Create_WithCustomLevel_ShouldSetLevel()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId);
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, WatchLevel.MentionsOnly);
        watcher.Level.Should().Be(WatchLevel.MentionsOnly);
    }

    [Fact]
    public void Create_ShouldRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId);
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.DomainEvents.Should().ContainSingle(e => e is ResourceWatchedDomainEvent);
    }

    [CoversMutation(typeof(ResourceWatcher), "Unwatch(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Unwatch_ShouldSetDeleted()
    {
        var watcher = CreateWatcher();
        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.IsDeleted.Should().BeTrue();
    }

    [CoversMutation(typeof(ResourceWatcher), "Unwatch(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Unwatch_ShouldRaiseEvent()
    {
        var watcher = CreateWatcher();
        ((IHasDomainEvents)watcher).ClearDomainEvents();
        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.DomainEvents.Should().ContainSingle(e => e is ResourceUnwatchedDomainEvent);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), Guid.NewGuid());
        var act = () => ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(ResourceWatcher), "Unwatch(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Unwatch_ShouldIncrementVersion()
    {
        var watcher = CreateWatcher();
        var before = watcher.Version;
        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        watcher.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(ResourceWatcher), "Unwatch(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Unwatch_WhenAlreadyDeleted_ShouldThrow()
    {
        var watcher = CreateWatcher();
        watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var act = () => watcher.Unwatch(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    private static ResourceWatcher CreateWatcher()
    {
        var workspaceId = Guid.NewGuid();
        return ResourceWatcher.Create(Guid.NewGuid(), workspaceId,
            ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId),
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
