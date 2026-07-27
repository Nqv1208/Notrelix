using FluentAssertions;
using Notrelix.Domain.WorkManagement.Labels;

namespace Notrelix.Domain.Tests.WorkManagement.Labels;

public class LabelEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Label_Restore_ShouldRaiseEvent()
    {
        var label = Label.Create(Guid.NewGuid(), WsA, BoardA, "Bug", LabelColor.Create("#FF0000"), Actor, Now);
        label.SoftDelete(Actor, Now);
        ((IHasDomainEvents)label).ClearDomainEvents();
        var version = label.Version;

        label.Restore(Actor, Now);

        label.IsDeleted.Should().BeFalse();
        label.Version.Should().Be(version + 1);
        label.DomainEvents.Should().ContainSingle(e => e is LabelRestoredDomainEvent);
    }

    [Fact]
    public void Label_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var label = Label.Create(Guid.NewGuid(), WsA, BoardA, "Bug", LabelColor.Create("#FF0000"), Actor, Now);
        ((IHasDomainEvents)label).ClearDomainEvents();
        var version = label.Version;

        label.Restore(Actor, Now);

        label.Version.Should().Be(version);
        label.DomainEvents.Should().NotContain(e => e is LabelRestoredDomainEvent);
    }
}
