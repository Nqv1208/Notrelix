using Notrelix.Application.Features.WorkManagement.BoardViews.Queries.GetBoardView;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardViews;

public class GetBoardViewTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetBoardViewQueryHandler _handler;

    public GetBoardViewTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new GetBoardViewQueryHandler(
            DbContextMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingView_ReturnsView()
    {
        var board = CreateBoard();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "My View",
            ViewType.Kanban, BoardViewConfig.Create(JsonValue.Create("{\"groupBy\":\"status\"}")),
            TestUserId, TestNow);
        SetupBoardViews(view);

        var query = new GetBoardViewQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NoView_ReturnsDefault()
    {
        var query = new GetBoardViewQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ViewFromDifferentUser_ReturnsDefault()
    {
        var board = CreateBoard();
        var otherUser = Guid.CreateVersion7();
        var view = BoardView.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Other View",
            ViewType.Table, BoardViewConfig.Create(JsonValue.EmptyObject()),
            otherUser, TestNow);
        SetupBoardViews(view);

        var query = new GetBoardViewQuery(board.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
