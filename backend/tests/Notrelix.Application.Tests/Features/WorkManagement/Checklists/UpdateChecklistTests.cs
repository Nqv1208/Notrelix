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

        var command = new UpdateChecklistCommand(checklist.Id, "New Title", 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        checklist.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_OmittedTitle_ChangesNothing()
    {
        var checklist = CreateChecklist();
        var version = checklist.Version;
        SetupChecklists(checklist);

        await _handler.Handle(new UpdateChecklistCommand(checklist.Id, null, 1), CancellationToken.None);

        checklist.Title.Should().Be("Test Checklist");
        checklist.Version.Should().Be(version, "placement is not part of PATCH; an empty PATCH is a no-op");
    }

    [Fact]
    public async Task Handle_ChecklistNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateChecklistCommand(Guid.CreateVersion7(), "Title", 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
