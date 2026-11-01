using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using BoardEntity = Notrelix.Domain.WorkManagement.Boards.Board;
using BoardMemberEntity = Notrelix.Domain.WorkManagement.Boards.BoardMember;
using NotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class UpdateBoardMemberRoleTests : WorkManagementHandlerTestBase
{
    private readonly UpdateBoardMemberRoleCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public UpdateBoardMemberRoleTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new UpdateBoardMemberRoleCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object,
            new BoardAccessChangeLog(new Mock<IResourceAccessChangeRecorder>().Object, new Mock<IAuditService>().Object, new Mock<ICurrentRequestContext>().Object),
            new Mock<Notrelix.Application.Features.Governance.Public.Access.IBoardSharingSummary>().Object,
            new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object,
            new Mock<Notrelix.Application.Common.Tenancy.IWorkspaceAccessResolver>().Object);
    }

    [Fact]
    public async Task Handle_MemberExists_UpdatesRole()
    {
        var board = CreateBoard();
        var member = BoardMemberEntity.Create(board.Id, TestUserId, BoardRole.Member, TestNow);
        SetupBoards(board);
        SetupBoardMembers(member);

        var command = new UpdateBoardMemberRoleCommand(board.Id, TestUserId, BoardRole.Admin, ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardMemberRoleCommand(Guid.CreateVersion7(), TestUserId, BoardRole.Admin, ExpectedVersion: 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MemberNotFound_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new UpdateBoardMemberRoleCommand(board.Id, Guid.CreateVersion7(), BoardRole.Admin, ExpectedVersion: 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    private Board CreateBoard(Guid? id = null)
    {
        var board = BoardEntity.Create(
            TestAccountId,
            TestWorkspaceId,
            TestUserId,
            "Test Board",
            null,
            TestNow,
            BoardVisibility.Workspace);
        if (id.HasValue)
            board.GetType().GetProperty(nameof(BoardEntity.Id))!.SetValue(board, id.Value);
        return board;
    }
}
