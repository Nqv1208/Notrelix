using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class UpdateChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateChecklistItemCommandHandler _handler;

    public UpdateChecklistItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateChecklistItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ToggleIsChecked_TogglesItem()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("b0"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklists(checklist);
        SetupChecklistItems(item);

        var command = new UpdateChecklistItemCommand(item.Id, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChecklistItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateChecklistItemCommand(Guid.CreateVersion7(), true);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullIsChecked_ReturnsSuccessWithoutToggle()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("b0"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklistItems(item);

        var command = new UpdateChecklistItemCommand(item.Id, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        item.Status.Should().Be(ChecklistItemStatus.Open);
    }

    [Fact]
    public async Task Handle_ChecklistNotFound_ThrowsNotFoundException()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Task", FractionalIndex.Create("b0"), TestUserId, TestNow);
        var item = checklist.Items.First();
        SetupChecklistItems(item);
        SetupChecklists();

        var command = new UpdateChecklistItemCommand(item.Id, true);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
