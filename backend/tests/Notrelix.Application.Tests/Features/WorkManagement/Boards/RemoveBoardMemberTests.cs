using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

public class RemoveBoardMemberTests : WorkManagementHandlerTestBase
{
    private readonly RemoveBoardMemberCommandHandler _handler;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IBoardSharingSummary> _sharing = new();

    public RemoveBoardMemberTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);

        _handler = new RemoveBoardMemberCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            new BoardAccessChangeLog(new Mock<IResourceAccessChangeRecorder>().Object, new Mock<IAuditService>().Object, new Mock<ICurrentRequestContext>().Object),
            DateTimeProviderMock.Object,
            _sharing.Object,
            new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object,
            new Mock<Notrelix.Application.Common.Tenancy.IWorkspaceAccessResolver>().Object);
    }

    [Fact]
    public async Task Handle_MemberExists_RemovesMember()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var member = BoardMember.Create(board.Id, TestUserId, BoardRole.Member, TestNow);
        SetupBoardMembers(member);

        var command = new RemoveBoardMemberCommand(board.Id, TestUserId, ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RemovingTheOnlyManager_IsRefusedWithTheRuleCode_AndTheMemberStays()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var manager = BoardMember.Create(board.Id, TestUserId, BoardRole.Admin, TestNow);
        SetupBoardMembers(manager);
        _sharing.Setup(s => s.GetBoardSharingAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SharingOf((TestUserId, AccessLevels.Manager)));

        var refused = await _handler.Invoking(h => h.Handle(new RemoveBoardMemberCommand(board.Id, TestUserId, ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();

        refused.Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Board_LastManager);
        DbContextMock.Object.BoardMembers.Should().Contain(manager, "a refused removal changes nothing");
    }

    [Fact]
    public async Task Handle_RemovingAManager_WhenAnotherManagerRemains_Succeeds()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var manager = BoardMember.Create(board.Id, TestUserId, BoardRole.Admin, TestNow);
        SetupBoardMembers(manager);
        _sharing.Setup(s => s.GetBoardSharingAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SharingOf((TestUserId, AccessLevels.Manager), (Guid.CreateVersion7(), AccessLevels.Manager)));

        var result = await _handler.Handle(new RemoveBoardMemberCommand(board.Id, TestUserId, ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        DbContextMock.Object.BoardMembers.Should().NotContain(manager);
    }

    [Fact]
    public async Task Handle_RemovingAnEditor_NeverAsksWhoManagesTheBoard()
    {
        var board = CreateBoard();
        SetupBoards(board);
        SetupBoardMembers(BoardMember.Create(board.Id, TestUserId, BoardRole.Member, TestNow));

        var result = await _handler.Handle(new RemoveBoardMemberCommand(board.Id, TestUserId, ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _sharing.Verify(s => s.GetBoardSharingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BoardNotFound_ThrowsNotFoundException()
    {
        var command = new RemoveBoardMemberCommand(Guid.CreateVersion7(), TestUserId, ExpectedVersion: 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsSuccess()
    {
        var board = CreateBoard();
        SetupBoards(board);

        var command = new RemoveBoardMemberCommand(board.Id, Guid.CreateVersion7(), ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardSharing SharingOf(params (Guid Id, string Level)[] entries) =>
        new(true, true, "workspace", null, null, null, entries.Length, 0, true,
            entries.Select(e => new BoardSharingEntry(
                "person", e.Id, "Person", e.Level, false, "member", null, null, "Admin", false, false, null, false, null, TestNow, null)).ToList(),
            [], []);
}
