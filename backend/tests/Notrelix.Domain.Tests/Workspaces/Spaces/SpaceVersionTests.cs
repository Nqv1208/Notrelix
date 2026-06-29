using FluentAssertions;
using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Domain.Tests.Workspaces;

public class SpaceVersionTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Original", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Rename("Renamed", _actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceRenamedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Archive(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceArchivedDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.SoftDelete(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.SoftDelete(_actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Restore(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredDomainEvent);
    }
}
