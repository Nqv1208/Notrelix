using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class DeleteChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly DeleteChecklistItemCommandHandler _handler;

    public DeleteChecklistItemTests()
    {
        _handler = new DeleteChecklistItemCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesChecklistItem()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("b0"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklistItems(item);

        var command = new DeleteChecklistItemCommand(item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChecklistItemNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteChecklistItemCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
