using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists.Events;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

/// <summary>WM-V3-APP-CHK-001: an item is removed through the owning Checklist.</summary>
public class DeleteChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DeleteChecklistItemCommandHandler _handler;

    public DeleteChecklistItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new DeleteChecklistItemCommandHandler(DbContextMock.Object, _currentUserMock.Object, DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_RemovesTheItemThroughTheChecklist()
    {
        var checklist = CreateChecklist();
        var item = checklist.AddItem("Task", FractionalIndex.Create("a1"), TestUserId, TestNow);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;
        SetupChecklists(checklist);

        await _handler.Handle(new DeleteChecklistItemCommand(item.Id, 1), CancellationToken.None);

        checklist.Items.Should().BeEmpty();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistItemRemovedDomainEvent);
    }

    [Fact]
    public async Task Handle_UnknownItem_ThrowsNotFound()
    {
        await _handler.Invoking(h => h.Handle(new DeleteChecklistItemCommand(Guid.CreateVersion7(), 1), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
