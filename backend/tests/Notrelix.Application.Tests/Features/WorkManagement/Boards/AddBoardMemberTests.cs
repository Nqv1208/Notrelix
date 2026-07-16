using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class AddBoardMemberTests : WorkManagementHandlerTestBase
{
    private readonly AddBoardMemberCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IWorkspaceAccessResolver> _workspaceAccessMock;

    public AddBoardMemberTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _workspaceAccessMock = new Mock<IWorkspaceAccessResolver>();
        _workspaceAccessMock
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(
                TestAccountId, TestWorkspaceId, TestUserId, CanAccess: true, IsWorkspaceActive: true));

        _handler = new AddBoardMemberCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object,
            _workspaceAccessMock.Object);
    }

    [Fact]
    public async Task Handle_NewMember_AddsMember()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new AddBoardMemberCommand(board.Id, Guid.CreateVersion7(), null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new AddBoardMemberCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserNotInWorkspace_ThrowsBusinessRuleViolationException()
    {
        var board = CreateBoard();
        SetupBoards(board);

        _workspaceAccessMock
            .Setup(r => r.ResolveAsync(board.WorkspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(
                TestAccountId, TestWorkspaceId, TestUserId, CanAccess: false, IsWorkspaceActive: true));

        var command = new AddBoardMemberCommand(board.Id, Guid.CreateVersion7(), null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Domain.Common.Exceptions.BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Handle_AlreadyMember_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var memberId = Guid.CreateVersion7();
        var member = BoardMember.Create(board.Id, memberId, BoardRole.Member, TestNow);
        SetupBoardMembers(member);

        var command = new AddBoardMemberCommand(board.Id, memberId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithRole_SetsRole()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new AddBoardMemberCommand(board.Id, Guid.CreateVersion7(), BoardRole.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
