using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UpdateBoardItemStatusTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateBoardItemStatusCommandHandler _handler;

    public UpdateBoardItemStatusTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateBoardItemStatusCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesStatus()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var statusField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        statusField.AddOption("Done", Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow, WorkStatusCategory.Done);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(statusField);

        var command = new UpdateBoardItemStatusCommand(item.Id, statusField.Options.First().Id.ToString());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardItemStatusCommand(Guid.CreateVersion7(), "Done");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NoStatusField_ReturnsFailure()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var textField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Title",
            FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(textField);

        var command = new UpdateBoardItemStatusCommand(item.Id, "Done");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeletedStatusField_ReturnsFailure()
    {
        var board = CreateBoard();
        var item = CreateBoardItem(boardId: board.Id);
        var statusField = BoardField.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Status",
            FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        statusField.Delete(TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardItems(item);
        SetupBoardFields(statusField);

        var command = new UpdateBoardItemStatusCommand(item.Id, "Done");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OnTheSystemStatusFieldOfAnAppCreatedBoard_WritesTheStatus()
    {
        var created = new List<BoardField>();
        var fieldSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<BoardField>>();
        fieldSet.Setup(s => s.AddRange(It.IsAny<BoardField[]>())).Callback<BoardField[]>(created.AddRange);
        DbContextMock.Setup(c => c.BoardFields).Returns(fieldSet.Object);
        var boardId = (await new Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace.CreateBoardInWorkspaceCommandHandler(
                DbContextMock.Object,
                Mock.Of<Notrelix.Application.Features.WorkManagement.Boards.Services.IBoardSpacePlacement>(),
                RequestContextMock.Object,
                DateTimeProviderMock.Object,
                Mock.Of<Notrelix.Application.Features.Identity.Public.Queries.IIdentityUserFacts>())
            .Handle(new Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace.CreateBoardInWorkspaceCommand(TestWorkspaceId, "App board", null, null, null), CancellationToken.None)).Data;
        var status = created.Single(f => f.Type == FieldType.Status);
        status.IsSystem.Should().BeTrue("the app creates the Status field as a system field");
        var item = CreateBoardItem(boardId: boardId);
        SetupBoardItems(item);
        SetupBoardFields(created.ToArray());
        var done = status.Options.Single(o => o.Category == WorkStatusCategory.Done).Id;

        var result = await _handler.Handle(new UpdateBoardItemStatusCommand(item.Id, done.ToString()), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        item.FieldValues.Should().ContainSingle(v => v.FieldId == status.Id).Which.Value.Data.Value.Should().Be($"\"{done}\"");
    }
}
