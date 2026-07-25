using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class RemoveBoardMemberTests : WorkManagementHandlerTestBase
{
    private readonly RemoveBoardMemberCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;

    public RemoveBoardMemberTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new RemoveBoardMemberCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_MemberExists_RemovesMember()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var member = BoardMember.Create(board.Id, TestUserId, BoardRole.Member, TestNow);
        SetupBoardMembers(member);

        var command = new RemoveBoardMemberCommand(board.Id, TestUserId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new RemoveBoardMemberCommand(Guid.CreateVersion7(), TestUserId);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new RemoveBoardMemberCommand(board.Id, Guid.CreateVersion7());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
