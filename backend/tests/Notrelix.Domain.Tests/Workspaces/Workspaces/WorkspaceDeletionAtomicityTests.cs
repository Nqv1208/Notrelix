using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces.Workspaces;

public class WorkspaceDeletionAtomicityTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Workspace CreateWorkspace() =>
        Workspace.Create(Guid.NewGuid(), ActorId, "Test Workspace", "test-ws", Now);

    [Fact]
    public void Delete_ShouldSetIsDeleted()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        workspace.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_ShouldSetDeleteAudit()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now, "reason");
        workspace.DeletedBy.Should().Be(ActorId);
        workspace.DeletedAt.Should().Be(Now);
    }

    [Fact]
    public void Delete_IsIdempotent_ShouldNotRaiseEvent()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        var eventsBefore = workspace.DomainEvents.Count;
        workspace.Delete(ActorId, Now);
        workspace.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [Fact]
    public void Delete_IsIdempotent_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        var before = workspace.Version;
        workspace.Delete(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [Fact]
    public void Restore_ShouldSetIsDeleted()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        workspace.Restore(ActorId, Now.AddMinutes(1));
        workspace.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldSetRestoreAudit()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        var actor = Guid.NewGuid();
        var time = Now.AddMinutes(2);
        workspace.Restore(actor, time);
    }

    [Fact]
    public void Restore_IsIdempotent_ShouldNotRaiseEvent()
    {
        var workspace = CreateWorkspace();
        workspace.Restore(ActorId, Now);
        var eventsBefore = workspace.DomainEvents.Count;
        workspace.Restore(ActorId, Now);
        workspace.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [Fact]
    public void Restore_IsIdempotent_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;
        workspace.Restore(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [Fact]
    public void Delete_RaisedEvent_ShouldContainAccountId()
    {
        var accountId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ActorId, "Test", "test", Now);
        workspace.Delete(ActorId, Now);
        var evt = workspace.DomainEvents.OfType<DomainEvent>().Last();
        evt.GetType().Name.Should().Be("WorkspaceDeletedDomainEvent");
    }

    [Fact]
    public void Restore_RaisedEvent_ShouldContainAccountId()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        workspace.Restore(ActorId, Now.AddMinutes(1));
        var evt = workspace.DomainEvents.OfType<DomainEvent>().Last();
        evt.GetType().Name.Should().Be("WorkspaceRestoredDomainEvent");
    }

    // ── Delete and restore rules (owner decision 2026-11-01) ─────────────

    [Fact]
    public void Delete_PersonalWorkspace_IsRefusedAndChangesNothing()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), ActorId, "Me", "personal-ws", Now, isPersonal: true);
        var version = workspace.Version;
        var events = workspace.DomainEvents.Count;

        var act = () => workspace.Delete(ActorId, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_CannotDeletePersonal);
        workspace.IsDeleted.Should().BeFalse();
        workspace.Version.Should().Be(version);
        workspace.DomainEvents.Count.Should().Be(events);
    }

    [Fact]
    public void Delete_AdvancesVersionOnce_AndSetsPurgeAfterThirtyDaysLater()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;

        workspace.Delete(ActorId, Now);

        workspace.Version.Should().Be(before + 1);
        workspace.PurgeAfter.Should().Be(Now.AddDays(30));
    }

    [Fact]
    public void Restore_AtTheEndOfTheWindow_Succeeds_AndKeepsTheStatus()
    {
        var workspace = CreateWorkspace();
        workspace.Archive(ActorId, Now);
        workspace.Delete(ActorId, Now);
        var deletedVersion = workspace.Version;

        workspace.Restore(ActorId, Now.AddDays(30));

        workspace.IsDeleted.Should().BeFalse();
        workspace.DeletedAt.Should().BeNull();
        workspace.Status.Should().Be(WorkspaceStatus.Archived);
        workspace.Version.Should().Be(deletedVersion + 1);
    }

    [Fact]
    public void Restore_AfterTheWindow_IsRefusedAndChangesNothing()
    {
        var workspace = CreateWorkspace();
        workspace.Delete(ActorId, Now);
        var version = workspace.Version;

        var act = () => workspace.Restore(ActorId, Now.AddDays(30).AddTicks(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_RestoreWindowExpired);
        workspace.IsDeleted.Should().BeTrue();
        workspace.Version.Should().Be(version);
    }

    [Fact]
    public void Restore_OfAnActiveWorkspace_IsANoOp()
    {
        var workspace = CreateWorkspace();
        var version = workspace.Version;
        var events = workspace.DomainEvents.Count;

        workspace.Restore(ActorId, Now.AddDays(90));

        workspace.Version.Should().Be(version);
        workspace.DomainEvents.Count.Should().Be(events);
    }
}
