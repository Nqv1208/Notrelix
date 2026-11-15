using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// The three message facts of a space share one receive endpoint and one partitioner (by space), so a create, an edit and a
/// delete of one space are delivered in the order the outbox committed them: a delete that overtook its create would show a
/// deleted message to a reader. Each frame carries the message sequence and version so a client orders by them as well.
/// </remarks>
internal static class ChatMessageRealtimePartitioning
{
    public const string EndpointName = "notrelix-chat-message-realtime-v1";
    public const int PartitionCount = 64;

    public static string Key(Guid spaceId) => spaceId.ToString("N");
}

[OrderingNotApplicable("Messages of one space are delivered in outbox commit order: the definition partitions by space on a shared endpoint, and each frame carries the message sequence and version so a client orders by them.")]
public sealed class ChatMessageCreatedRealtimeConsumer : IConsumer<ChatMessageCreatedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatMessageCreatedRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatMessageCreatedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForCreated(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

/// <summary>
/// The per-user unread frames of a new message (UC-CHT-C09), on their own endpoint. They count each recipient's backlog, which can take
/// seconds in a busy channel, and the channel frame of the message must not wait behind that count (nor the next message's frame behind this one).
/// </summary>
[OrderingNotApplicable("Each frame carries the absolute unread and mention count read at delivery, so a later delivery supersedes an earlier one; the definition partitions by space so one space's counts are computed one after the other.")]
public sealed class ChatMessageCreatedUnreadConsumer : IConsumer<ChatMessageCreatedIntegrationEvent>
{
    private readonly ChatUnreadFanOut _unread;

    public ChatMessageCreatedUnreadConsumer(ChatUnreadFanOut unread) => _unread = unread;

    public Task Consume(ConsumeContext<ChatMessageCreatedIntegrationEvent> context) =>
        _unread.NotifyCreatedAsync(context.Message, DateTimeOffset.UtcNow, context.CancellationToken);
}

[OrderingNotApplicable("Messages of one space are delivered in outbox commit order: the definition partitions by space on a shared endpoint, and each frame carries the message sequence and version so a client orders by them.")]
public sealed class ChatMessageUpdatedRealtimeConsumer : IConsumer<ChatMessageUpdatedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatMessageUpdatedRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatMessageUpdatedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForUpdated(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

[OrderingNotApplicable("Messages of one space are delivered in outbox commit order: the definition partitions by space on a shared endpoint, and each frame carries the message sequence and version so a client orders by them.")]
public sealed class ChatMessageDeletedRealtimeConsumer : IConsumer<ChatMessageDeletedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatMessageDeletedRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatMessageDeletedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForDeleted(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

public sealed class ChatMessageCreatedRealtimeConsumerDefinition : ConsumerDefinition<ChatMessageCreatedRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageCreatedRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatMessageCreatedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}

public sealed class ChatMessageCreatedUnreadConsumerDefinition : ConsumerDefinition<ChatMessageCreatedUnreadConsumer>
{
    public ChatMessageCreatedUnreadConsumerDefinition() => EndpointName = "notrelix-chat-message-unread-fan-out-v1";

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageCreatedUnreadConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatMessageCreatedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}

public sealed class ChatMessageUpdatedRealtimeConsumerDefinition : ConsumerDefinition<ChatMessageUpdatedRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageUpdatedRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatMessageUpdatedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}

public sealed class ChatMessageDeletedRealtimeConsumerDefinition : ConsumerDefinition<ChatMessageDeletedRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageDeletedRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatMessageDeletedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}
