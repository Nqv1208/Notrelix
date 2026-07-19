using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class ArchiveBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly ArchiveBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public ArchiveBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new ArchiveBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_GroupExists_ArchivesGroup()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new ArchiveBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new ArchiveBoardGroupCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyArchived_IsIdempotent()
    {
        var group = CreateBoardGroup();
        group.Archive(TestUserId, TestNow);
        SetupBoardGroups(group);

        var command = new ArchiveBoardGroupCommand(group.Id);

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
