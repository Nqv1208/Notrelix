using FluentAssertions;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Checklists;

public class ChecklistEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Checklist), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Checklist_Delete_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.Delete(Actor, Now);

        checklist.IsDeleted.Should().BeTrue();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistDeletedDomainEvent);
    }

    [CoversMutation(typeof(Checklist), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void Checklist_Delete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.Delete(Actor, Now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.Delete(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistDeletedDomainEvent);
    }

    [CoversMutation(typeof(Checklist), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Checklist_Restore_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.Delete(Actor, Now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.IsDeleted.Should().BeFalse();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistRestoredDomainEvent);
    }

    [CoversMutation(typeof(Checklist), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Checklist_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistRestoredDomainEvent);
    }
}
