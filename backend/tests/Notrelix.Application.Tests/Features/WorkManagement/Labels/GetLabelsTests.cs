using Notrelix.Application.Features.WorkManagement.Labels.Queries.GetLabels;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class GetLabelsTests : WorkManagementHandlerTestBase
{
    private readonly GetLabelsQueryHandler _handler;

    public GetLabelsTests()
    {
        _handler = new GetLabelsQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsLabelsForBoard()
    {
        var boardId = Guid.CreateVersion7();
        var label1 = Label.Create(
            TestAccountId, TestWorkspaceId, boardId,
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        var label2 = Label.Create(
            TestAccountId, TestWorkspaceId, boardId,
            "Feature", LabelColor.Create("#00FF00"), TestUserId, TestNow);
        SetupLabels(label1, label2);

        var query = new GetLabelsQuery(boardId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoLabels_ReturnsEmptyList()
    {
        var query = new GetLabelsQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyReturnsLabelsForSpecifiedBoard()
    {
        var boardId = Guid.CreateVersion7();
        var otherBoardId = Guid.CreateVersion7();
        var label1 = Label.Create(
            TestAccountId, TestWorkspaceId, boardId,
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        var label2 = Label.Create(
            TestAccountId, TestWorkspaceId, otherBoardId,
            "Other", LabelColor.Create("#0000FF"), TestUserId, TestNow);
        SetupLabels(label1, label2);

        var query = new GetLabelsQuery(boardId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data!.First().Name.Should().Be("Bug");
    }
}
