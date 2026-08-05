using Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class GetChecklistsTests : WorkManagementHandlerTestBase
{
    private readonly GetChecklistsQueryHandler _handler;

    public GetChecklistsTests()
    {
        _handler = new GetChecklistsQueryHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsChecklistsForItem()
    {
        var itemId = Guid.CreateVersion7();
        var checklist = CreateChecklist(itemId: itemId);
        checklist.AddItem("Task 1", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupChecklists(checklist);

        var items = checklist.Items.ToArray();
        SetupChecklistItems(items);

        var query = new GetChecklistsQuery(itemId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be("Test Checklist");
        result.Data.First().Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoChecklists_ReturnsEmptyList()
    {
        var query = new GetChecklistsQuery(Guid.CreateVersion7());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleChecklists_ReturnsOrderedByPosition()
    {
        var itemId = Guid.CreateVersion7();
        var checklist1 = CreateChecklist(itemId: itemId);
        checklist1.Rename("First", TestUserId, TestNow);
        checklist1.UpdatePosition(FractionalIndex.Create("a0"), TestUserId, TestNow);

        var checklist2 = CreateChecklist(itemId: itemId);
        checklist2.Rename("Second", TestUserId, TestNow);
        checklist2.UpdatePosition(FractionalIndex.Create("a1"), TestUserId, TestNow);

        SetupChecklists(checklist1, checklist2);

        var query = new GetChecklistsQuery(itemId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data.Should().HaveCount(2);
    }
}
