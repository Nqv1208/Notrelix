using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Original", "original", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Rename("Renamed", _actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceRenamedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Archive(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceArchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        workspace.Archive(_actorId, _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Unarchive(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceUnarchivedDomainEvent);
    }

    [Fact]
    public void UpdateDescription_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.UpdateDescription("New description", _actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceDescriptionUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateSettings_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;
        var settings = WorkspaceSettings.Create(allowPublicSharing: true);

        workspace.UpdateSettings(settings, _actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceSettingsUpdatedDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.SoftDelete(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        workspace.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Restore(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.IsDeleted.Should().BeFalse();
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceRestoredDomainEvent);
    }
}
