using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>An export lists the live sub-items of each exported item right after it, with the parent's key; archived ones are left out.</summary>
public class BoardExportSubItemsTests : WorkManagementHandlerTestBase
{
    private static BoardItemSummaryDto Summary(BoardItem item, string key) =>
        new(item.Id, item.Name, 0, [], [], 0, 0, 0, 0, "a0", DateTime.UtcNow, null, 1, ItemKey: key, ParentItemId: item.ParentItemId);

    [Fact]
    public async Task Build_ListsLiveSubItemsAfterTheirParent_WithTheParentKey_AndSkipsArchivedOnes()
    {
        var parent = CreateBoardItem();
        var live = BoardItem.CreateSubItem(parent, "Live", FractionalIndex.Create("a0"), TestUserId, TestNow);
        var archived = BoardItem.CreateSubItem(parent, "Archived", FractionalIndex.Create("a1"), TestUserId, TestNow);
        archived.Archive(TestUserId, TestNow);
        SetupBoardItems(parent, live, archived);
        SetupBoardFields();
        SetupBoardGroups();

        var pages = new Mock<IBoardItemPageReader>();
        pages.Setup(p => p.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CriteriaField?>(), It.IsAny<IReadOnlyList<CriteriaField>>(),
                It.IsAny<BoardItemCriteria>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new BoardItemLanePage("", [parent.Id], 1, 1, null)]);
        var summaries = new Mock<IBoardItemSummaryBuilder>();
        summaries.Setup(b => b.BuildAsync(It.IsAny<IReadOnlyList<BoardField>>(), It.IsAny<IReadOnlyList<BoardItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BoardField> _, IReadOnlyList<BoardItem> items, CancellationToken _) =>
                items.Select(i => Summary(i, i.Id == parent.Id ? "P-1" : "P-2")).ToList());

        var table = await new BoardExportBuilder(DbContextMock.Object, pages.Object, summaries.Object, Mock.Of<IActorLookupService>())
            .BuildAsync(parent.BoardId, BoardExportBuilder.ScopeAll, null, null, "en", null, CancellationToken.None);

        table.Headers[10].Should().Be("Parent");
        table.Rows.Select(r => r[0]).Should().Equal("P-1", "P-2");
        table.Rows[0][10].Should().BeNull();
        table.Rows[1][10].Should().Be("P-1");
    }
}
