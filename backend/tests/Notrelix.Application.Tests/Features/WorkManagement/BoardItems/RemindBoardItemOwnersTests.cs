using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RemindBoardItemOwners;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.WorkManagement.Items.Events;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class RemindBoardItemOwnersTests : WorkManagementHandlerTestBase
{
    private readonly InMemoryBoardItemReminders _reminders = new();
    private readonly RemindBoardItemOwnersCommandHandler _handler;
    private readonly Guid _colleague = Guid.CreateVersion7();

    public RemindBoardItemOwnersTests()
    {
        _handler = new RemindBoardItemOwnersCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _reminders);
    }

    private BoardItem ItemInCharge(params Guid[] assignees)
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        SetupBoardItemMembers(assignees.Select(a => CreateBoardItemMember(itemId: item.Id, userId: a)).ToArray());
        return item;
    }

    private static List<BoardItemOwnersRemindedDomainEvent> Reminded(BoardItem item) =>
        item.DomainEvents.OfType<BoardItemOwnersRemindedDomainEvent>().ToList();

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
        var fact = Reminded(item).Should().ContainSingle().Which;
        fact.SenderUserId.Should().Be(TestUserId);
        fact.OccurredAt.Should().Be(TestNow);
        fact.RecipientUserIds.Should().BeEquivalentTo([_colleague, other]);
        _reminders.RemindedAt(item.Id, TestUserId).Should().Be(TestNow);
    }

    [Fact]
    public async Task Handle_AgainWithin24Hours_IsAConflictThatSaysWhenAndSendsNothing()
    {
        var item = ItemInCharge(_colleague);
        _reminders.Seed(item.Id, TestUserId, TestNow.AddHours(-23));

        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<ConflictException>()).Which;
        thrown.ErrorCode.Should().Be(RemindBoardItemOwnersCommand.TooSoonErrorCode);
        thrown.Message.Should().Contain(TestNow.AddHours(1).ToString("O"));
        Reminded(item).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_After24Hours_RemindsAgainAndMovesTheLimit()
    {
        var item = ItemInCharge(_colleague);
        _reminders.Seed(item.Id, TestUserId, TestNow.AddHours(-24));

        var result = await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _reminders.RemindedAt(item.Id, TestUserId).Should().Be(TestNow);
        Reminded(item).Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_AnotherSendersReminderDoesNotUseUpTheCallersLimit()
    {
        var item = ItemInCharge(_colleague);
        _reminders.Seed(item.Id, Guid.CreateVersion7(), TestNow.AddHours(-1));

        var result = await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NobodyElseInCharge_IsRejectedAndNeverRemindsTheCaller()
    {
        var item = ItemInCharge(TestUserId);

        var act = () => _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        Reminded(item).Should().BeEmpty();
        _reminders.Count.Should().Be(0);
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

    [Fact]
    public async Task Handle_SimultaneousPresses_ExactlyOneWinsAndOnlyTheWinnerRecordsTheFact()
    {
        var item = ItemInCharge(_colleague);

        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            try
            {
                return (await _handler.Handle(new RemindBoardItemOwnersCommand(item.Id), CancellationToken.None)).Succeeded;
            }
            catch (ConflictException ex) when (ex.ErrorCode == RemindBoardItemOwnersCommand.TooSoonErrorCode)
            {
                return false;
            }
        }));

        attempts.Count(won => won).Should().Be(1);
        Reminded(item).Should().ContainSingle();
    }
}
