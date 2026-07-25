using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class AssignBoardItemMemberTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IWorkspaceAccessResolver> _workspaceAccessMock = new();
    private readonly AssignBoardItemMemberCommandHandler _handler;

    public AssignBoardItemMemberTests()
    {
        _handler = new AssignBoardItemMemberCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object,
            _workspaceAccessMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsMember()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var targetUserId = Guid.CreateVersion7();
        _workspaceAccessMock
            .Setup(w => w.ResolveAsync(TestWorkspaceId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, targetUserId, true, true));

        var command = new AssignBoardItemMemberCommand(item.Id, targetUserId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new AssignBoardItemMemberCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserNotInWorkspace_ThrowsForbiddenException()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var targetUserId = Guid.CreateVersion7();
        _workspaceAccessMock
            .Setup(w => w.ResolveAsync(TestWorkspaceId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, targetUserId, false, true));

        var command = new AssignBoardItemMemberCommand(item.Id, targetUserId);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_AlreadyAssigned_ReturnsSuccess()
    {
        var item = CreateBoardItem();
        var targetUserId = Guid.CreateVersion7();
        var existingMember = CreateBoardItemMember(itemId: item.Id, userId: targetUserId);
        SetupBoardItems(item);
        SetupBoardItemMembers(existingMember);
        _workspaceAccessMock
            .Setup(w => w.ResolveAsync(TestWorkspaceId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceAccessSnapshot(TestAccountId, TestWorkspaceId, targetUserId, true, true));

        var command = new AssignBoardItemMemberCommand(item.Id, targetUserId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
