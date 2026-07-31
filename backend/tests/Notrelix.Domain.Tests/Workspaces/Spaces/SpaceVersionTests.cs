using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

public class SpaceVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Space), nameof(Space.Rename), MutationScenario.Version, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var space = Space.Create(_accountId, _workspaceId, "Original", SpaceVisibility.Private, _actorId, _now);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var version = space.Version;

        space.Rename("Renamed", _actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceRenamedDomainEvent);
    }

    [CoversMutation(typeof(Space), nameof(Space.Archive), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var space = Space.Create(_accountId, _workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var version = space.Version;

        space.Archive(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceArchivedDomainEvent);
    }

    [CoversMutation(typeof(Space), nameof(Space.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var space = Space.Create(_accountId, _workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var version = space.Version;

        space.Delete(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceDeletedDomainEvent);
    }

    [CoversMutation(typeof(Space), nameof(Space.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var space = Space.Create(_accountId, _workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.Delete(_actorId, _now);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var version = space.Version;

        space.Restore(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredDomainEvent);
    }
}
