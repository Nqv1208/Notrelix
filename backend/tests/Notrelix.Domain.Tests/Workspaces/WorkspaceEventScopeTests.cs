using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceEventScopeTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void WorkspaceCreated_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceCreatedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceRenamed_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Rename("New Name", ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceRenamedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceArchived_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Archive(ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceArchivedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceUnarchived_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        workspace.Archive(ActorId, Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Unarchive(ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceUnarchivedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceDeleted_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Delete(ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceDeletedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceRestored_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        workspace.Delete(ActorId, Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Restore(ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceRestoredDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceDescriptionUpdated_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.UpdateDescription("New desc", ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceDescriptionUpdatedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public void WorkspaceSettingsUpdated_ShouldCarryCorrectAccountId()
    {
        var workspace = Workspace.Create(AccountId, ActorId, "Test", "test", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var newSettings = WorkspaceSettings.Create(allowPublicSharing: true);
        workspace.UpdateSettings(newSettings, ActorId, Now);

        var evt = workspace.DomainEvents.OfType<WorkspaceSettingsUpdatedDomainEvent>().Single();
        evt.AccountId.Should().Be(AccountId);
        evt.WorkspaceId.Should().Be(workspace.Id);
    }
}
