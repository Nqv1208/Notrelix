using FluentAssertions;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Labels;

public class LabelEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Label), nameof(Label.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Label), nameof(Label.Update), MutationScenario.Event, typeof(string), typeof(LabelColor), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Label_Restore_ShouldRaiseEvent()
    {
        var label = Label.Create(Guid.NewGuid(), WsA, BoardA, "Bug", LabelColor.Create("#FF0000"), Actor, Now);
        label.Delete(Actor, Now);
        ((IHasDomainEvents)label).ClearDomainEvents();
        var version = label.Version;

        label.Restore(Actor, Now);

        label.IsDeleted.Should().BeFalse();
        label.Version.Should().Be(version + 1);
        label.DomainEvents.Should().ContainSingle(e => e is LabelRestoredDomainEvent);
    }

    [CoversMutation(typeof(Label), nameof(Label.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Label), nameof(Label.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
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
