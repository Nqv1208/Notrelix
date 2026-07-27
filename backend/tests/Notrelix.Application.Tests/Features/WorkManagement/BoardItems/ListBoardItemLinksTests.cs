using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemLinks;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class ListBoardItemLinksTests : WorkManagementHandlerTestBase
{
    private readonly ListBoardItemLinksQueryHandler _handler;

    public ListBoardItemLinksTests()
    {
        _handler = new ListBoardItemLinksQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_LinksExist_ReturnsLinks()
    {
        var sourceItemId = Guid.CreateVersion7();
        var link = CreateBoardItemLink(sourceItemId: sourceItemId);
        SetupBoardItemLinks(link);

        var query = new ListBoardItemLinksQuery(sourceItemId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoLinks_ReturnsEmptyList()
    {
        var query = new ListBoardItemLinksQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
