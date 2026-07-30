using FluentAssertions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Collaboration;

public class ResourceWatcherTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId);

        var watcher = ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        watcher.WorkspaceId.Should().Be(workspaceId);
        watcher.Target.Should().Be(target);
        watcher.Level.Should().Be(WatchLevel.All);
        watcher.DomainEvents.Should().ContainSingle(e => e is ResourceWatchedDomainEvent);
    }

    [Fact]
    public void Create_WithSpecificLevel_ShouldSetLevel()
    {
        var watcher = ResourceWatcher.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid()), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, WatchLevel.MentionsOnly);

        watcher.Level.Should().Be(WatchLevel.MentionsOnly);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), Guid.NewGuid());

        var act = () => ResourceWatcher.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
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

    private static ResourceWatcher CreateWatcher()
    {
        var workspaceId = Guid.NewGuid();
        return ResourceWatcher.Create(Guid.NewGuid(), workspaceId, ResourceRef.Create(ResourceType.Board, Guid.NewGuid(), workspaceId), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }
}
