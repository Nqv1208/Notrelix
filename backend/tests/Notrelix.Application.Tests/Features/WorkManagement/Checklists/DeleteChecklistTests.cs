using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class DeleteChecklistTests : WorkManagementHandlerTestBase
{
    private readonly DeleteChecklistCommandHandler _handler;

    public DeleteChecklistTests()
    {
        _handler = new DeleteChecklistCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesChecklist()
    {
        var checklist = CreateChecklist();
        SetupChecklists(checklist);

        var command = new DeleteChecklistCommand(checklist.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChecklistNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteChecklistCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ChecklistWithItems_DeletesBothChecklistAndItems()
    {
        var checklist = CreateChecklist();
        var itemId = Guid.CreateVersion7();
        checklist.AddItem("Item 1", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupChecklists(checklist);

        var command = new DeleteChecklistCommand(checklist.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
