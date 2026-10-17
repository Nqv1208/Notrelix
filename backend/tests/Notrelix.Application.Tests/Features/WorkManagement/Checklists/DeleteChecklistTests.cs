using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;
using Notrelix.Domain.WorkManagement.Checklists.Events;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

/// <summary>WM-V3-APP-CHK-001: deleting a checklist is the aggregate's soft delete.</summary>
public class DeleteChecklistTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DeleteChecklistCommandHandler _handler;

    public DeleteChecklistTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new DeleteChecklistCommandHandler(DbContextMock.Object, _currentUserMock.Object, DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_SoftDeletesTheChecklist_WithoutRemovingItemRows()
    {
        var checklist = CreateChecklist();
        checklist.AddItem("Item 1", FractionalIndex.Create("a1"), TestUserId, TestNow);
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        SetupChecklists(checklist);

        await _handler.Handle(new DeleteChecklistCommand(checklist.Id), CancellationToken.None);

        checklist.IsDeleted.Should().BeTrue();
        checklist.Items.Should().ContainSingle();
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistDeletedDomainEvent);
    }

    [Fact]
    public async Task Handle_ChecklistNotFound_ThrowsNotFoundException()
    {
        await _handler.Invoking(h => h.Handle(new DeleteChecklistCommand(Guid.CreateVersion7()), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
