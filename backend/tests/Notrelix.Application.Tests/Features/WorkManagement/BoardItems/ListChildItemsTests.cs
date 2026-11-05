using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListChildItems;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>The sub-items of an item in their order: archived and deleted children are left out, done ones are marked.</summary>
public class ListChildItemsTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly BoardItem _parent;

    public ListChildItemsTests()
    {
        _parent = CreateBoardItem();
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SetupBoardItemMembers();
    }

    private BoardItem Child(string name, string position) =>
        BoardItem.CreateSubItem(_parent, name, FractionalIndex.Create(position), TestUserId, TestNow);

    private Task<ChildItemListDto> List(Guid parentId) =>
        new ListChildItemsQueryHandler(DbContextMock.Object, _actors.Object).Handle(new ListChildItemsQuery(parentId), CancellationToken.None);

    [Fact]
    public async Task List_ReturnsTheLiveChildrenInOrder_WithDoneMarked_AndLeavesArchivedAndDeletedOut()
    {
        var first = Child("First", "a0");
        var done = Child("Done", "a1");
        done.Complete(TestNow, TestUserId, TestNow);
        var archived = Child("Archived", "a2");
        archived.Archive(TestUserId, TestNow);
        var deleted = Child("Deleted", "a3");
        deleted.Delete(TestUserId, TestNow);
        SetupBoardItems(_parent, first, done, archived, deleted);

        var list = await List(_parent.Id);

        list.Items.Select(i => i.Title).Should().Equal("First", "Done");
        list.Items.Select(i => i.Done).Should().Equal(false, true);
    }

    [Fact]
    public async Task List_ForAMissingParent_IsNotFound()
    {
        SetupBoardItems(_parent);

        await new Func<Task>(() => List(Guid.CreateVersion7()))
            .Should().ThrowAsync<NotFoundException>();
    }
}
