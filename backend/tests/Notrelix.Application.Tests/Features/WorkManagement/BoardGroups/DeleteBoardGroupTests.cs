using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class DeleteBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly DeleteBoardGroupCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public DeleteBoardGroupTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new DeleteBoardGroupCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_GroupExists_SoftDeletesGroup()
    {
        var group = CreateBoardGroup();
        SetupBoardGroups(group);

        var command = new DeleteBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteBoardGroupCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_IsIdempotent()
    {
        var group = CreateBoardGroup();
        group.SoftDelete(TestUserId, TestNow);
        SetupBoardGroups(group);

        var command = new DeleteBoardGroupCommand(group.Id);

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
