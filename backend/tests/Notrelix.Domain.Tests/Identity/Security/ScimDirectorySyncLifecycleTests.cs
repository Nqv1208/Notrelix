using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class ScimDirectorySyncLifecycleTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldRaiseCreatedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);

        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncCreatedDomainEvent);
        var evt = (ScimDirectorySyncCreatedDomainEvent)sync.DomainEvents.Single(e => e is ScimDirectorySyncCreatedDomainEvent);
        evt.SyncId.Should().Be(sync.Id);
        evt.ProviderName.Should().Be("Azure AD");
    }

    [Fact]
    public void Pause_ShouldRaisePausedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Pause(_actorId, _now);

        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncPausedDomainEvent);
    }

    [Fact]
    public void Resume_ShouldRaiseResumedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.Pause(_actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Resume(_actorId, _now);

        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncResumedDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.SoftDelete(_actorId, _now);

        sync.IsDeleted.Should().BeTrue();
        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var sync = ScimDirectorySync.Create(workspaceId, "Azure AD", _actorId, _now);
        sync.SoftDelete(_actorId, _now);
        sync.ClearDomainEvents();
        var version = sync.Version;

        sync.Restore(_actorId, _now);

        sync.IsDeleted.Should().BeFalse();
        sync.Version.Should().Be(version + 1);
        sync.DomainEvents.Should().ContainSingle(e => e is ScimDirectorySyncRestoredDomainEvent);
    }
}
