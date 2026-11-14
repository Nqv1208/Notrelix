using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// Reaction changes share the message endpoint and its partitioner (by space), so a reaction and a delete of one space reach the
/// channel stream in the order the outbox committed them. The frame is compact and carries no counts: a reader applies the change or
/// refetches the message's reaction summary over REST.
/// </remarks>
[OrderingNotApplicable("Reaction changes of one space are delivered in outbox commit order on the shared chat message endpoint (partitioned by space); the frame is compact and readers refetch the summary.")]
public sealed class ChatReactionRealtimeConsumer : IConsumer<ChatReactionChangedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatReactionRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatReactionChangedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForReactionChanged(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

public sealed class ChatReactionRealtimeConsumerDefinition : ConsumerDefinition<ChatReactionRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatReactionRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatReactionChangedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}
