using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using ViewMode = Notrelix.Domain.WorkManagement.ViewMode;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class SaveBoardViewTests : WorkManagementHandlerTestBase
{
    private readonly SaveBoardViewCommandHandler _handler;

    public SaveBoardViewTests()
    {
        _handler = new SaveBoardViewCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_NewView_CreatesBoardView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Table, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new SaveBoardViewCommand(Guid.CreateVersion7(), ViewMode.Kanban, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithFilters_SavesFilters()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var filters = "{\"status\":\"active\"}";

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Table, filters);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_KanbanMode_CreatesKanbanView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.Kanban, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(ViewType.Table)]
    [InlineData(ViewType.Kanban)]
    public async Task Handle_ExistingView_SavesWipLimits_AndKeepsOtherKeys(ViewType existingType)
    {
        var board = CreateBoard();
        SetupBoards(board);
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View", existingType,
            BoardViewConfig.Create(JsonValue.Create("{\"laneCardLimit\":3}")), TestUserId, TestNow);
        SetupBoardViews(view);
        var lane = Guid.CreateVersion7().ToString();
        var config = "{\"laneCardLimit\":5,\"wipLimits\":{\"" + lane + "\":2}}";

        var result = await _handler.Handle(new SaveBoardViewCommand(board.Id, ViewMode.Kanban, config), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        view.Config.Data.Value.Should().Be(JsonValue.Create(config).Value);
        KanbanWipLimits.FromConfig(view.Config.Data).LimitFor(lane).Should().Be(2);
    }

    [Fact]
    public async Task Handle_ExistingView_ConfigWithoutWipLimits_StillSaves()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View", ViewType.Kanban,
            BoardViewConfig.Create(JsonValue.EmptyObject()), TestUserId, TestNow);
        SetupBoardViews(view);

        var result = await _handler.Handle(new SaveBoardViewCommand(board.Id, ViewMode.Kanban, "{\"groupBy\":\"status\"}"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        view.Config.Data.Value.Should().Be("{\"groupBy\":\"status\"}");
        KanbanWipLimits.FromConfig(view.Config.Data).Should().Be(KanbanWipLimits.None);
    }

    [Fact]
    public async Task Handle_WipLimitOutOfRange_IsRejected_AndViewUnchanged()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View", ViewType.Kanban,
            BoardViewConfig.Create(JsonValue.EmptyObject()), TestUserId, TestNow);
        SetupBoardViews(view);
        var versionBefore = view.Version;

        var act = () => _handler.Handle(
            new SaveBoardViewCommand(board.Id, ViewMode.Kanban, "{\"wipLimits\":{\"lane\":1000}}"), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.WorkManagement.WorkManagementRuleCodes.WorkManagement_Kanban_WipLimitOutOfRange);
        view.Config.Data.Value.Should().Be("{}");
        view.Version.Should().Be(versionBefore);
    }

    [Fact]
    public async Task Handle_ListMode_CreatesTableView()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new SaveBoardViewCommand(board.Id, ViewMode.List, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
