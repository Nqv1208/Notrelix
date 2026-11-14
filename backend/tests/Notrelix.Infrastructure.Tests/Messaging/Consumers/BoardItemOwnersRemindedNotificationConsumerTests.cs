using MassTransit;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// UC-COL-D08: a nudge reaches each person in charge of the Item, and never the person who sent it. The sender is
/// filtered out before the notifier is asked, so a reminder that lists the sender among its recipients is still
/// delivered only to the others.
/// </summary>
public sealed class BoardItemOwnersRemindedNotificationConsumerTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Item = Guid.NewGuid();
    private static readonly Guid Board = Guid.NewGuid();
    private static readonly Guid Sender = Guid.NewGuid();
    private static readonly Guid AssigneeA = Guid.NewGuid();
    private static readonly Guid AssigneeB = Guid.NewGuid();

    [Fact]
    public async Task Reminder_ReachesEachAssignee()
    {
        var (sent, consumer) = Build();

        await consumer.Consume(ConsumeFor(Message(AssigneeA, AssigneeB)));

        sent.Should().BeEquivalentTo(new[] { AssigneeA, AssigneeB });
    }

    [Fact]
    public async Task Reminder_NeverGoesToTheSender_EvenWhenTheSenderIsListed()
    {
        var (sent, consumer) = Build();

        await consumer.Consume(ConsumeFor(Message(Sender, AssigneeA)));

        sent.Should().Equal(AssigneeA);
    }

    [Fact]
    public async Task Reminder_ToOnlyTheSender_NotifiesNobody()
    {
        var (sent, consumer) = Build();

        await consumer.Consume(ConsumeFor(Message(Sender)));

        sent.Should().BeEmpty();
    }

    private static (List<Guid> Sent, BoardItemOwnersRemindedNotificationConsumer Consumer) Build()
    {
        var notifier = new Mock<ItemFollowerNotifier>(null!, null!, null!, null!);
        var sent = new List<Guid>();
        notifier.Setup(n => n.NotifyAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, Guid?, Guid, string, string, string?, IReadOnlyCollection<Guid>, DateTimeOffset, CancellationToken>(
                (_, _, _, _, _, _, _, _, recipients, _, _) => sent.AddRange(recipients))
            .Returns(Task.CompletedTask);

        return (sent, new BoardItemOwnersRemindedNotificationConsumer(notifier.Object));
    }

    private static BoardItemOwnersRemindedIntegrationEvent Message(params Guid[] recipients) => new(
        Guid.NewGuid(), Account, Workspace, Item, Board, Sender, recipients, Guid.NewGuid());

    private static ConsumeContext<BoardItemOwnersRemindedIntegrationEvent> ConsumeFor(BoardItemOwnersRemindedIntegrationEvent message)
    {
        var context = new Mock<ConsumeContext<BoardItemOwnersRemindedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }
}
