using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UnassignBoardItemMemberTests : WorkManagementHandlerTestBase
{
    private readonly UnassignBoardItemMemberCommandHandler _handler;

    public UnassignBoardItemMemberTests()
    {
        _handler = new UnassignBoardItemMemberCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_MemberExists_RemovesMember()
    {
        var item = CreateBoardItem();
        var targetUserId = Guid.CreateVersion7();
        var member = CreateBoardItemMember(itemId: item.Id, userId: targetUserId);
        SetupBoardItemMembers(member);

        var command = new UnassignBoardItemMemberCommand(item.Id, targetUserId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsSuccess()
    {
        var command = new UnassignBoardItemMemberCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
