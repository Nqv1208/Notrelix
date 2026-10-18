using FluentAssertions;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement.Items;

/// <summary>
/// Item keys come from the Board's one sequence for root and child Items alike and
/// never change when the Item moves.
/// </summary>
public class BoardItemKeyTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private static BoardItem Root(Board board, Guid groupId, string position)
    {
        var (sequence, key) = board.GenerateNextItemIdentity(Actor, Now);
        return BoardItem.CreateRoot(Account, Workspace, board.Id, groupId, $"Item {sequence}", FractionalIndex.Create(position), Actor, Now, key, sequence);
    }

    private static ItemParentPath ParentPath(BoardItem parent) =>
        ItemParentPath.Create(Account, Workspace, parent.BoardId, parent.Id, parent.ItemLevel, []);

    [Fact]
    public void RootAndChildItems_DrawSequentialKeys_FromTheSameBoardSequence()
    {
        var board = Board.Create(Account, Workspace, Actor, "Board", null, Now, itemKeyPrefix: "WEB");
        var groupId = Guid.NewGuid();

        var parent = Root(board, groupId, "a0");
        var (childSequence, childKey) = board.GenerateNextItemIdentity(Actor, Now);
        var child = BoardItem.CreateChild(ParentPath(parent), groupId, "Child", FractionalIndex.Create("a1"), Actor, Now, childKey, childSequence);
        var sibling = Root(board, groupId, "a2");

        new[] { parent.ItemKey, child.ItemKey, sibling.ItemKey }.Should().Equal("WEB-1", "WEB-2", "WEB-3");
        new long?[] { parent.ItemSequence, child.ItemSequence, sibling.ItemSequence }.Should().Equal(1, 2, 3);
        board.ItemSequence.Should().Be(3);
    }

    [Fact]
    public void ABoardWithoutPrefix_GivesPlainNumbers()
    {
        var board = Board.Create(Account, Workspace, Actor, "Board", null, Now);

        var first = Root(board, Guid.NewGuid(), "a0");
        var second = Root(board, Guid.NewGuid(), "a1");

        first.ItemKey.Should().Be("1");
        second.ItemKey.Should().Be("2");
    }

    [Fact]
    public void MovingAnItem_KeepsItsKey()
    {
        var board = Board.Create(Account, Workspace, Actor, "Board", null, Now, itemKeyPrefix: "OPS");
        var from = Guid.NewGuid();
        var item = Root(board, from, "a0");
        var parent = Root(board, from, "a1");
        var target = new BoardGroupRef(Account, Workspace, board.Id, Guid.NewGuid());

        item.MoveToGroup(target, FractionalIndex.Create("a5"), Actor, Now.AddMinutes(1));
        item.MoveUnder(ParentPath(parent), Actor, Now.AddMinutes(2));
        item.MoveToRoot(Actor, Now.AddMinutes(3));

        item.ItemKey.Should().Be("OPS-1");
        item.ItemSequence.Should().Be(1);
    }
}
