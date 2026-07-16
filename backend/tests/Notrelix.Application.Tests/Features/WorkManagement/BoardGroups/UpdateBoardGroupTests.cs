using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class UpdateBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly UpdateBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public UpdateBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UpdateBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task HandleWithTitle_UpdatesTitle()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, "Updated Title");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleWithColor_UpdatesColor()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, null, "#FF0000");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardGroupCommand(Guid.CreateVersion7(), "Title");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithBothTitleAndColor_UpdatesBoth()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, "New Title", "#00FF00");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullTitleAndColor_NoChanges()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new UpdateBoardGroupCommand(group.Id, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardGroup CreateBoardGroup(Guid? id = null)
    {
        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(
            TestAccountId,
            TestWorkspaceId,
            Guid.CreateVersion7(),
            "Test Group",
            Color.Create("#808080"),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            group.GetType().GetProperty(nameof(BoardGroup.Id))!.SetValue(group, id.Value);
        return group;
    }
}
