using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.WorkManagement.Boards;

public class BoardEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Board), nameof(Board.UpdateDescription), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Board_UpdateDescription_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("New desc", Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardDescriptionUpdatedDomainEvent);
        var evt = (BoardDescriptionUpdatedDomainEvent)board.DomainEvents.Single(e => e is BoardDescriptionUpdatedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.BoardId.Should().Be(board.Id);
        evt.OldDescription.Should().BeNull();
        evt.NewDescription.Should().Be("New desc");
    }

    [CoversMutation(typeof(Board), nameof(Board.UpdateDescription), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Board_UpdateDescription_WhenSameValue_ShouldNotRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", "desc", Now);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("desc", Actor, Now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardDescriptionUpdatedDomainEvent);
    }

    [CoversMutation(typeof(Board), nameof(Board.UpdateBackground), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Board_UpdateBackground_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;

        board.UpdateBackground("new-bg", Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardBackgroundUpdatedDomainEvent);
        var evt = (BoardBackgroundUpdatedDomainEvent)board.DomainEvents.Single(e => e is BoardBackgroundUpdatedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.BoardId.Should().Be(board.Id);
        evt.NewBackground.Should().Be("new-bg");
    }

    [CoversMutation(typeof(Board), nameof(Board.SetDefaultGroup), MutationScenario.Event, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Board_SetDefaultGroup_ShouldRaiseEvent()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();
        var version = board.Version;
        var groupId = Guid.NewGuid();

        board.SetDefaultGroup(groupId, Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardDefaultGroupSetDomainEvent);
        var evt = (BoardDefaultGroupSetDomainEvent)board.DomainEvents.Single(e => e is BoardDefaultGroupSetDomainEvent);
        evt.GroupId.Should().Be(groupId);
    }
}
