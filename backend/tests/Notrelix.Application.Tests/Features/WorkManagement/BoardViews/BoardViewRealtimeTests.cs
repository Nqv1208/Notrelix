using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UpdateBoardViewConfig;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

/// <summary>The board's realtime topic reaches every member: a private view's name and settings never ride on it.</summary>
public class BoardViewRealtimeTests : WorkManagementHandlerTestBase
{
    private readonly IntegrationEventCollector _events = new();

    private IExecutionContextReader Context()
    {
        var context = new Mock<IExecutionContextReader>();
        context.Setup(c => c.AccountId).Returns(TestAccountId);
        context.Setup(c => c.WorkspaceId).Returns(TestWorkspaceId);
        context.Setup(c => c.UserId).Returns(TestUserId);
        return context.Object;
    }

    private CreateBoardViewCommandHandler CreateHandler() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        new CreateBoardViewRealtimeMapper(Context(), DateTimeProviderMock.Object), _events);

    private UpdateBoardViewConfigCommandHandler UpdateHandler()
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(c => c.UserId).Returns(TestUserId);
        return new(
            DbContextMock.Object, user.Object, DateTimeProviderMock.Object,
            new UpdateBoardViewConfigRealtimeMapper(Context(), DateTimeProviderMock.Object), _events);
    }

    private int Pending() => _events.CapturePending().Events.Count;

    [Theory]
    [InlineData("Board", 1)]
    [InlineData("Private", 0)]
    public async Task CreatingAView_BroadcastsOnlyAViewTheBoardShares(string visibility, int broadcast)
    {
        var board = CreateBoard();
        SetupBoards(board);

        await CreateHandler().Handle(new CreateBoardViewCommand(board.Id, "My View", "Table", "{}", visibility), CancellationToken.None);

        Pending().Should().Be(broadcast);
    }

    [Theory]
    [InlineData(BoardViewVisibility.Board, 1)]
    [InlineData(BoardViewVisibility.Private, 0)]
    public async Task ChangingAViewsSettings_BroadcastsOnlyAViewTheBoardShares(BoardViewVisibility visibility, int broadcast)
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View", ViewType.Table,
            BoardViewConfig.Create(JsonValue.EmptyObject()), TestUserId, TestNow, visibility: visibility);
        SetupBoardViews(view);

        await UpdateHandler().Handle(new UpdateBoardViewConfigCommand(board.Id, view.Id, "{\"hidden\":[]}", view.Version), CancellationToken.None);

        Pending().Should().Be(broadcast);
    }

    [Fact]
    public async Task ASaveThatChangesNothing_BumpsNoVersion_AndTellsNoOne()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "View", ViewType.Table,
            BoardViewConfig.Create(JsonValue.Create("{\"hidden\":[]}")), TestUserId, TestNow);
        SetupBoardViews(view);
        var before = view.Version;

        await UpdateHandler().Handle(new UpdateBoardViewConfigCommand(board.Id, view.Id, "{\"hidden\":[]}", before), CancellationToken.None);

        view.Version.Should().Be(before);
        Pending().Should().Be(0);
    }
}
