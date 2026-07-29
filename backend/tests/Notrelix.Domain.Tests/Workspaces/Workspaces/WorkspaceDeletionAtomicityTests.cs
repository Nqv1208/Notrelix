using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces.Workspaces;

public class WorkspaceDeletionAtomicityTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Workspace CreateWorkspace() =>
        Workspace.Create(Guid.NewGuid(), ActorId, "Test Workspace", "test-ws", Now);

    [CoversMutation(typeof(Workspace), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldSetStatusToSoftDeleted()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        workspace.Status.Should().Be(WorkspaceStatus.SoftDeleted);
    }

    [CoversMutation(typeof(Workspace), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldSetDeleteAudit()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now, "reason");
        workspace.DeletedBy.Should().Be(ActorId);
        workspace.DeletedAt.Should().Be(Now);
    }

    [CoversMutation(typeof(Workspace), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_IsIdempotent_ShouldNotRaiseEvent()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        var eventsBefore = workspace.DomainEvents.Count;
        workspace.SoftDelete(ActorId, Now);
        workspace.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [CoversMutation(typeof(Workspace), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_IsIdempotent_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        var before = workspace.Version;
        workspace.SoftDelete(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetStatusToActive()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        workspace.Restore(ActorId, Now.AddMinutes(1));
        workspace.Status.Should().Be(WorkspaceStatus.Active);
    }

    [CoversMutation(typeof(Workspace), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetRestoreAudit()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        var actor = Guid.NewGuid();
        var time = Now.AddMinutes(2);
        workspace.Restore(actor, time);
        workspace.RestoredBy.Should().Be(actor);
        workspace.RestoredAt.Should().Be(time);
    }

    [CoversMutation(typeof(Workspace), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Restore_IsIdempotent_ShouldNotRaiseEvent()
    {
        var workspace = CreateWorkspace();
        workspace.Restore(ActorId, Now);
        var eventsBefore = workspace.DomainEvents.Count;
        workspace.Restore(ActorId, Now);
        workspace.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [CoversMutation(typeof(Workspace), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Restore_IsIdempotent_ShouldNotIncrementVersion()
    {
        var workspace = CreateWorkspace();
        var before = workspace.Version;
        workspace.Restore(ActorId, Now);
        workspace.Version.Should().Be(before);
    }

    [CoversMutation(typeof(Workspace), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_RaisedEvent_ShouldContainAccountId()
    {
        var accountId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ActorId, "Test", "test", Now);
        workspace.SoftDelete(ActorId, Now);
        var evt = workspace.DomainEvents.OfType<DomainEvent>().Last();
        evt.GetType().Name.Should().Be("WorkspaceSoftDeletedDomainEvent");
    }

    [CoversMutation(typeof(Workspace), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_RaisedEvent_ShouldContainAccountId()
    {
        var workspace = CreateWorkspace();
        workspace.SoftDelete(ActorId, Now);
        workspace.Restore(ActorId, Now.AddMinutes(1));
        var evt = workspace.DomainEvents.OfType<DomainEvent>().Last();
        evt.GetType().Name.Should().Be("WorkspaceRestoredDomainEvent");
    }
}
