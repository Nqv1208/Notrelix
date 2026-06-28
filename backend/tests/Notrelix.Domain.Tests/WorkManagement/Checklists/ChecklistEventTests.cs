using FluentAssertions;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Domain.Tests.WorkManagement.Checklists;

public class ChecklistEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Checklist_SoftDelete_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.SoftDelete(Actor, Now);

        checklist.IsDeleted.Should().BeTrue();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistSoftDeletedDomainEvent);
    }

    [Fact]
    public void Checklist_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.SoftDelete(Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.SoftDelete(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistSoftDeletedDomainEvent);
    }

    [Fact]
    public void Checklist_Restore_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.SoftDelete(Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.IsDeleted.Should().BeFalse();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistRestoredDomainEvent);
    }

    [Fact]
    public void Checklist_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistRestoredDomainEvent);
    }
}
