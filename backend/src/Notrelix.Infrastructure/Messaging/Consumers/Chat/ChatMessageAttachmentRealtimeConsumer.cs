using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// File changes share the message endpoint and its partitioner (by space), so a file change, an edit and a delete of one space reach the
/// channel stream in the order the outbox committed them. The frame is compact metadata (no bytes, no URL beyond the authorized route).
/// </remarks>
[OrderingNotApplicable("File changes of one space are delivered in outbox commit order on the shared chat message endpoint (partitioned by space); each frame is added-by-id or removed-by-id, so a reader applies it idempotently.")]
public sealed class ChatMessageAttachmentRealtimeConsumer : IConsumer<ChatMessageAttachmentChangedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatMessageAttachmentRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatMessageAttachmentChangedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForAttachmentChanged(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

public sealed class ChatMessageAttachmentRealtimeConsumerDefinition : ConsumerDefinition<ChatMessageAttachmentRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageAttachmentRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatMessageAttachmentChangedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}
