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
    public void Delete_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Delete(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        workspace.Delete(_actorId, _now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var version = workspace.Version;

        workspace.Restore(_actorId, _now);

        workspace.Version.Should().Be(version + 1);
        workspace.IsDeleted.Should().BeFalse();
        workspace.DomainEvents.Should().Contain(e => e is WorkspaceRestoredDomainEvent);
    }

    [Fact]
    public void UpdateSettings_FlippingPublicSharing_RecordsTheInstantAndKeepsItAcrossOtherWrites()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        workspace.Settings.PublicSharingChangedAt.Should().BeNull("a new Workspace starts with links off and no recorded flip");

        var onAt = _now.AddHours(1);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: true), _actorId, onAt);
        workspace.Settings.PublicSharingChangedAt.Should().Be(onAt);

        var otherAt = _now.AddHours(2);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true), _actorId, otherAt);
        workspace.Settings.PublicSharingChangedAt.Should().Be(onAt, "a write that does not flip the policy keeps the instant");

        var offAt = _now.AddHours(3);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: false, enforceMfa: true), _actorId, offAt);
        workspace.Settings.PublicSharingChangedAt.Should().Be(offAt);
    }

    [Fact]
    public void UpdateSettings_SemanticNoOp_KeepsVersionAndInstant()
    {
        var workspace = Workspace.Create(_accountId, _actorId, "Workspace", "workspace", _now);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: true), _actorId, _now.AddHours(1));
        var version = workspace.Version;

        workspace.UpdateSettings(WorkspaceSettings.Create(allowPublicSharing: true), _actorId, _now.AddHours(2));

        workspace.Version.Should().Be(version);
        workspace.Settings.PublicSharingChangedAt.Should().Be(_now.AddHours(1));
    }
}
