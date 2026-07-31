using FluentAssertions;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Views;

public class BoardViewEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(BoardView), nameof(BoardView.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardView_Restore_ShouldRaiseEvent()
    {
        var config = BoardViewConfig.Create(JsonValue.EmptyObject());
        var view = BoardView.Create(Guid.NewGuid(), WsA, BoardA, "View", ViewType.Table, config, Actor, Now);
        view.Delete(Actor, Now);
        ((IHasDomainEvents)view).ClearDomainEvents();
        var version = view.Version;

        view.Restore(Actor, Now);

        view.IsDeleted.Should().BeFalse();
        view.Version.Should().Be(version + 1);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewRestoredDomainEvent);
    }

    [CoversMutation(typeof(BoardView), nameof(BoardView.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardView_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var config = BoardViewConfig.Create(JsonValue.EmptyObject());
        var view = BoardView.Create(Guid.NewGuid(), WsA, BoardA, "View", ViewType.Table, config, Actor, Now);
        ((IHasDomainEvents)view).ClearDomainEvents();
        var version = view.Version;

        view.Restore(Actor, Now);

        view.Version.Should().Be(version);
        view.DomainEvents.Should().NotContain(e => e is BoardViewRestoredDomainEvent);
    }
}
