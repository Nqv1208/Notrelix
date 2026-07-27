using Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class ToggleChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ToggleChecklistItemCommandHandler _handler;

    public ToggleChecklistItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new ToggleChecklistItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_TogglesChecklistItem()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("a1"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklists(checklist);

        var command = new ToggleChecklistItemCommand(item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChecklistItemNotFound_ThrowsNotFoundException()
    {
        var command = new ToggleChecklistItemCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ToggleTwice_ReturnsToOriginalState()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("a1"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklists(checklist);

        var command = new ToggleChecklistItemCommand(item.Id);

        await _handler.Handle(command, CancellationToken.None);
        await _handler.Handle(command, CancellationToken.None);

        item.Status.Should().Be(ChecklistItemStatus.Open);
    }
}
