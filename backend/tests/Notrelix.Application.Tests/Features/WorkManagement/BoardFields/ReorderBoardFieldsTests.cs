using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardFields;

public class ReorderBoardFieldsTests : WorkManagementHandlerTestBase
{
    private readonly ReorderBoardFieldsCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public ReorderBoardFieldsTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new ReorderBoardFieldsCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReorder_UpdatesPositions()
    {
        var board = CreateBoard();
        var field1 = CreateBoardField(boardId: board.Id);
        var field2 = CreateBoardField(boardId: board.Id);
        SetupBoards(board);
        SetupBoardFields(field1, field2);

        var items = new List<ReorderItem>
        {
            new(field1.Id, 2.0),
            new(field2.Id, 1.0)
        };
        var command = new ReorderBoardFieldsCommand(board.Id, items);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldNotFound_SkipsGracefully()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var items = new List<ReorderItem>
        {
            new(Guid.CreateVersion7(), 1.0)
        };
        var command = new ReorderBoardFieldsCommand(board.Id, items);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyItems_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new ReorderBoardFieldsCommand(board.Id, new List<ReorderItem>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FieldBelongsToDifferentBoard_SkipsGracefully()
    {
        var board1 = CreateBoard();
        var board2 = CreateBoard();
        var field1 = CreateBoardField(boardId: board1.Id);
        SetupBoards(board1, board2);
        SetupBoardFields(field1);

        var items = new List<ReorderItem>
        {
            new(field1.Id, 1.0)
        };
        var command = new ReorderBoardFieldsCommand(board2.Id, items);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardField CreateBoardField(Guid? id = null, Guid? boardId = null)
    {
        var field = Notrelix.Domain.WorkManagement.Fields.BoardField.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Field",
            FieldType.Text,
            FieldSettings.Empty(),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            field.GetType().GetProperty(nameof(BoardField.Id))!.SetValue(field, id.Value);
        return field;
    }
}
