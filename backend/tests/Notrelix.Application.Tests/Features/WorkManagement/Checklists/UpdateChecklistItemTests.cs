using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

/// <summary>WM-V3-APP-CHK-001: PATCH sets the desired state; it never toggles.</summary>
public class UpdateChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateChecklistItemCommandHandler _handler;

    public UpdateChecklistItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateChecklistItemCommandHandler(DbContextMock.Object, _currentUserMock.Object, DateTimeProviderMock.Object);
    }

    private (Checklist Checklist, ChecklistItem Item) Arrange()
    {
        var checklist = CreateChecklist();
        var item = checklist.AddItem("Task", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupChecklists(checklist);
        return (checklist, item);
    }

    [Fact]
    public async Task Handle_IsCheckedTrue_ChecksTheItem_AndRepeatingIsANoOp()
    {
        var (checklist, item) = Arrange();

        await _handler.Handle(new UpdateChecklistItemCommand(item.Id, 1, IsChecked: true), CancellationToken.None);
        var versionAfterFirst = checklist.Version;
        await _handler.Handle(new UpdateChecklistItemCommand(item.Id, 1, IsChecked: true), CancellationToken.None);

        item.Status.Should().Be(ChecklistItemStatus.Done, "a repeated desired state must not invert the item");
        checklist.Version.Should().Be(versionAfterFirst);
    }

    [Fact]
    public async Task Handle_IsCheckedFalse_OnAnOpenItem_IsANoOp()
    {
        var (checklist, item) = Arrange();
        var version = checklist.Version;

        await _handler.Handle(new UpdateChecklistItemCommand(item.Id, 1, IsChecked: false), CancellationToken.None);

        item.Status.Should().Be(ChecklistItemStatus.Open);
        checklist.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_Title_RenamesTheItem_AndOmittedFieldsAreUnchanged()
    {
        var (_, item) = Arrange();

        await _handler.Handle(new UpdateChecklistItemCommand(item.Id, 1, Title: " Renamed "), CancellationToken.None);

        item.Title.Should().Be("Renamed");
        item.Status.Should().Be(ChecklistItemStatus.Open);
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsNotFound()
    {
        Arrange();

        await _handler.Invoking(h => h.Handle(new UpdateChecklistItemCommand(Guid.CreateVersion7(), 1, IsChecked: true), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
