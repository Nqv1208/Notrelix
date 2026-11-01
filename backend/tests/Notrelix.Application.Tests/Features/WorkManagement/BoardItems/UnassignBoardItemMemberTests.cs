using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UnassignBoardItemMemberTests : WorkManagementHandlerTestBase
{
    private readonly UnassignBoardItemMemberCommandHandler _handler;

    public UnassignBoardItemMemberTests()
    {
        _handler = new UnassignBoardItemMemberCommandHandler(DbContextMock.Object, new Mock<ICurrentUser>().Object, DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_MemberExists_RemovesMember()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var targetUserId = Guid.CreateVersion7();
        var member = CreateBoardItemMember(itemId: item.Id, userId: targetUserId);
        SetupBoardItemMembers(member);

        var command = new UnassignBoardItemMemberCommand(item.Id, targetUserId, ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsSuccess()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var command = new UnassignBoardItemMemberCommand(item.Id, Guid.CreateVersion7(), ExpectedVersion: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
