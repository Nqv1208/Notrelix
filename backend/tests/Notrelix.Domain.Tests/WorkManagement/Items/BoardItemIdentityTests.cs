using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

public class BoardItemIdentityTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void GenerateNextItemIdentity_FirstItem_ShouldReturnSequenceOne()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        sequence.Should().Be(1);
        key.Should().Be("1");
        board.Version.Should().Be(2);
    }

    [Fact]
    public void GenerateNextItemIdentity_WithPrefix_ShouldApplyPrefix()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now, itemKeyPrefix: "TASK");
        ((IHasDomainEvents)board).ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        sequence.Should().Be(1);
        key.Should().Be("TASK-1");
    }

    [Fact]
    public void GenerateNextItemIdentity_ShouldIncrementSequentially()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var (seq1, _) = board.GenerateNextItemIdentity(Actor, Now);
        var (seq2, key2) = board.GenerateNextItemIdentity(Actor, Now);

        seq1.Should().Be(1);
        seq2.Should().Be(2);
        key2.Should().Be("2");
    }

    [Fact]
    public void GenerateNextItemIdentity_ShouldEmitEvent()
    {
        var board = Board.Create(Guid.NewGuid(), WsA, Actor, "Board", null, Now);
        ((IHasDomainEvents)board).ClearDomainEvents();

        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);

        var ev = board.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<BoardItemIdentityGeneratedDomainEvent>().Subject;
        ev.BoardId.Should().Be(board.Id);
        ev.SequenceNumber.Should().Be(sequence);
        ev.ItemKey.Should().Be(key);
        ev.WorkspaceId.Should().Be(WsA);
        ev.UpdatedBy.Should().Be(Actor);
    }
}
