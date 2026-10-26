using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RemindBoardItemOwners;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class RemindBoardItemOwnersTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IItemReminderNotifier> _notifier = new();
    private readonly RemindBoardItemOwnersCommandHandler _handler;
    private readonly Guid _colleague = Guid.CreateVersion7();

    public RemindBoardItemOwnersTests()
    {
        _handler = new RemindBoardItemOwnersCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _notifier.Object);
    }

    private BoardItem ItemInCharge(params Guid[] assignees)
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        SetupBoardItemMembers(assignees.Select(a => CreateBoardItemMember(itemId: item.Id, userId: a)).ToArray());
        return item;
    }

    [Fact]
    public async Task Authorization_NeedsOnlyToSeeTheItem()
    {
        var command = new RemindBoardItemOwnersCommand(Guid.CreateVersion7());

        command.Action.Should().Be(PermissionAction.ViewBoard);
        command.Resource.ResourceId.Should().Be(command.BoardItemId);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_FirstReminder_NotifiesEveryoneInChargeExceptTheCallerAndStartsTheLimit()
    {
        var other = Guid.CreateVersion7();
        var item = ItemInCharge(_colleague, other, TestUserId);

        var result = await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Recipients.Should().Be(2);
        result.Data.NextRemindAt.Should().Be(TestNow.AddHours(24));
        _notifier.Verify(n => n.NotifyAsync(
            TestAccountId, TestWorkspaceId, item.Id, TestUserId, It.IsAny<Guid>(), TestNow,
            It.Is<IReadOnlyCollection<Guid>>(r => r.Count == 2 && r.Contains(_colleague) && r.Contains(other) && !r.Contains(TestUserId)),
            It.IsAny<CancellationToken>()), Times.Once);
        DbContextMock.Object.BoardItemReminders.Should().ContainSingle(r => r.ItemId == item.Id && r.SenderUserId == TestUserId && r.RemindedAt == TestNow);
    }

    [Fact]
    public async Task Handle_AgainWithin24Hours_IsAConflictThatSaysWhenAndSendsNothing()
    {
        var item = ItemInCharge(_colleague);
        SetupBoardItemReminders(BoardItemReminder.Create(TestAccountId, TestWorkspaceId, item.BoardId, item.Id, TestUserId, TestNow.AddHours(-23)));

        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<ConflictException>()).Which;
        thrown.ErrorCode.Should().Be(RemindBoardItemOwnersCommand.TooSoonErrorCode);
        thrown.Message.Should().Contain(TestNow.AddHours(1).ToString("O"));
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_After24Hours_RemindsAgainAndMovesTheLimit()
    {
        var item = ItemInCharge(_colleague);
        var previous = BoardItemReminder.Create(TestAccountId, TestWorkspaceId, item.BoardId, item.Id, TestUserId, TestNow.AddHours(-24));
        SetupBoardItemReminders(previous);

        var result = await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        previous.RemindedAt.Should().Be(TestNow);
        _notifier.Verify(n => n.NotifyAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), item.Id, TestUserId, previous.Id, TestNow,
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AnotherSendersReminderDoesNotUseUpTheCallersLimit()
    {
        var item = ItemInCharge(_colleague);
        SetupBoardItemReminders(BoardItemReminder.Create(TestAccountId, TestWorkspaceId, item.BoardId, item.Id, Guid.CreateVersion7(), TestNow.AddHours(-1)));

        var result = await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NobodyElseInCharge_IsRejectedAndNeverRemindsTheCaller()
    {
        var item = ItemInCharge(TestUserId);

        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        _notifier.VerifyNoOtherCalls();
        DbContextMock.Object.BoardItemReminders.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownItem_IsNotFound()
    {
        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ArchivedItem_IsRejected()
    {
        var item = ItemInCharge(_colleague);
        item.Archive(TestUserId, TestNow);

        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }
}
