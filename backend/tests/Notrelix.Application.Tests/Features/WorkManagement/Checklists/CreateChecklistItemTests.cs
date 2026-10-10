using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists.Events;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

/// <summary>WM-V3-APP-CHK-001: a new item is added through the owning Checklist.</summary>
public class CreateChecklistItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateChecklistItemCommandHandler _handler;

    public CreateChecklistItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new CreateChecklistItemCommandHandler(DbContextMock.Object, _currentUserMock.Object, DateTimeProviderMock.Object, Placement);
    }

    [Fact]
    public async Task Handle_AddsTheItemThroughTheChecklist_WithOneVersionAndEvent()
    {
        var checklist = CreateChecklist();
        ((IHasDomainEvents)checklist).ClearDomainEvents();
        var version = checklist.Version;
        SetupChecklists(checklist);

        var result = await _handler.Handle(new CreateChecklistItemCommand(checklist.Id, "  Buy milk "), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        checklist.Items.Should().ContainSingle(i => i.Id == result.Data).Which.Title.Should().Be("Buy milk");
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistItemAddedDomainEvent);
    }

    [Fact]
    public async Task Handle_AppendsAfterTheLastExistingItem()
    {
        var checklist = CreateChecklist();
        var first = checklist.AddItem("First", FractionalIndex.Create("a1"), TestUserId, TestNow);
        SetupChecklists(checklist);
        SetupChecklistItems(first);

        var result = await _handler.Handle(new CreateChecklistItemCommand(checklist.Id, "Second"), CancellationToken.None);

        checklist.Items.Single(i => i.Id == result.Data).Position.CompareTo(first.Position).Should().BePositive();
    }

    [Fact]
    public async Task Handle_UnknownChecklist_ThrowsNotFound_AndAddsNothing()
    {
        await _handler.Invoking(h => h.Handle(new CreateChecklistItemCommand(Guid.CreateVersion7(), "Task"), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
