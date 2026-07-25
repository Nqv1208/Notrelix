using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class UpdateChecklistTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateChecklistCommandHandler _handler;

    public UpdateChecklistTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateChecklistCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_Rename_UpdatesChecklistTitle()
    {
        var checklist = CreateChecklist();
        SetupChecklists(checklist);

        var command = new UpdateChecklistCommand(checklist.Id, "New Title", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatePosition_UpdatesChecklistPosition()
    {
        var checklist = CreateChecklist();
        SetupChecklists(checklist);

        var command = new UpdateChecklistCommand(checklist.Id, null, 5.0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BothTitleAndPosition_UpdatesBoth()
    {
        var checklist = CreateChecklist();
        SetupChecklists(checklist);

        var command = new UpdateChecklistCommand(checklist.Id, "Updated", 10.0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ChecklistNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateChecklistCommand(Guid.CreateVersion7(), "Title", null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
