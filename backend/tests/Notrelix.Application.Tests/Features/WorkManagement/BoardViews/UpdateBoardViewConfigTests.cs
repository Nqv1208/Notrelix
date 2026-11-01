using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class UpdateBoardViewConfigTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateBoardViewConfigCommandHandler _handler;

    public UpdateBoardViewConfigTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateBoardViewConfigCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesConfig()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View",
            ViewType.Form, BoardViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId, TestNow);
        SetupBoardViews(view);

        var command = new UpdateBoardViewConfigCommand(board.Id, view.Id, "{\"questions\":[\"name\"]}", ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(view.Id);
        result.Config.Should().Be("{\"questions\":[\"name\"]}");
    }

    [Fact]
    public async Task Handle_KanbanView_WipLimits_RoundTripInResponse()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Kanban",
            ViewType.Kanban, BoardViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId, TestNow);
        SetupBoardViews(view);
        var config = "{\"wipLimits\":{\"lane-a\":4}}";

        var result = await _handler.Handle(new UpdateBoardViewConfigCommand(board.Id, view.Id, config, ExpectedVersion: 1), CancellationToken.None);

        result.Config.Should().Be(config);
        KanbanWipLimits.FromConfig(view.Config.Data).LimitFor("lane-a").Should().Be(4);
    }

    [Fact]
    public async Task Handle_BoardViewNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardViewConfigCommand(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "{}", ExpectedVersion: 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ViewIdMismatch_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View",
            ViewType.Form, BoardViewConfig.Create(JsonValue.EmptyObject()),
            TestUserId, TestNow);
        SetupBoardViews(view);

        var command = new UpdateBoardViewConfigCommand(board.Id, Guid.CreateVersion7(), "{}", ExpectedVersion: 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
