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

    [CoversMutation(typeof(Label), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(Label), "Update(System.String,Notrelix.Domain.WorkManagement.Labels.LabelColor,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(Label), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(Label), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
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
