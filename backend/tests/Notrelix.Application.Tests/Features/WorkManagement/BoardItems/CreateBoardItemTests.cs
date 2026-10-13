using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class CreateBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly CreateBoardItemCommandHandler _handler;

    public CreateBoardItemTests()
    {
        _handler = new CreateBoardItemCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object,
            Placement,
            KeyAllocator);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesItem()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Test Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);

        var command = new CreateBoardItemCommand(board.Id, group.Id, "New Item");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Item");
        result.GroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new CreateBoardItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), "New Item");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupBelongsToDifferentBoard_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var otherBoard = CreateBoard();
        var group = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, otherBoard.Id, "Other Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board, otherBoard);
        SetupBoardGroups(group);

        var command = new CreateBoardItemCommand(board.Id, group.Id, "New Item");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}

/// <summary>
/// Every created Item gets a Board-scoped key from the Board sequence in the same
/// unit of work; the Board version advances with the sequence.
/// </summary>
public class CreateBoardItemKeyTests : WorkManagementHandlerTestBase
{
    private readonly List<BoardItem> _items = [];

    public CreateBoardItemKeyTests()
    {
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(_items));
    }

    private (Board Board, BoardGroup Group) Seed(string? prefix)
    {
        var board = Board.Create(TestAccountId, TestWorkspaceId, TestUserId, "Board", null, TestNow, itemKeyPrefix: prefix);
        var group = BoardGroup.Create(TestAccountId, TestWorkspaceId, board.Id, "Group",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);
        return (board, group);
    }

    [Fact]
    public async Task Create_AssignsSequentialPrefixedKeys_WithoutTouchingTheBoardAggregate()
    {
        var (board, group) = Seed("WEB");
        var version = board.Version;
        var handler = new CreateBoardItemCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

        await handler.Handle(new CreateBoardItemCommand(board.Id, group.Id, "First"), CancellationToken.None);
        await handler.Handle(new CreateBoardItemCommand(board.Id, group.Id, "Second"), CancellationToken.None);

        _items.Select(i => (i.Name, i.ItemKey, i.ItemSequence)).Should().Equal(("First", "WEB-1", 1L), ("Second", "WEB-2", 2L));
        board.Version.Should().Be(version, "reserving keys does not change the Board aggregate");
        board.DomainEvents.Should().NotContain(e => e is BoardItemIdentityGeneratedDomainEvent);
    }

    [Fact]
    public async Task Create_OnAnArchivedBoard_IsRejected()
    {
        var (board, group) = Seed("WEB");
        board.Archive(TestUserId, TestNow);
        var handler = new CreateBoardItemCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

        var act = () => handler.Handle(new CreateBoardItemCommand(board.Id, group.Id, "Late"), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.WorkManagement.WorkManagementRuleCodes.WorkManagement_Board_CannotGenerateIdentityArchived);
        _items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_OnABoardWithoutPrefix_AssignsPlainNumbers()
    {
        var (board, group) = Seed(null);
        var handler = new CreateBoardItemCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

        await handler.Handle(new CreateBoardItemCommand(board.Id, group.Id, "Only"), CancellationToken.None);

        _items.Single().ItemKey.Should().Be("1");
    }

    [Fact]
    public async Task Duplicate_GivesTheCopyItsOwnNewKey()
    {
        var (board, group) = Seed("OPS");
        var create = new CreateBoardItemCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);
        await create.Handle(new CreateBoardItemCommand(board.Id, group.Id, "Source"), CancellationToken.None);
        var source = _items.Single();
        var duplicate = new Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem.DuplicateBoardItemCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

        await duplicate.Handle(new Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem.DuplicateBoardItemCommand(source.Id), CancellationToken.None);

        _items.Select(i => i.ItemKey).Should().Equal("OPS-1", "OPS-2");
    }
}
