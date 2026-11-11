using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// Pin changes share the message endpoint and its partitioner (by space), so a pin and a delete of one space reach the channel
/// stream in the order the outbox committed them. The frame is notify-only: readers refetch the pins over REST.
/// </remarks>
[OrderingNotApplicable("Pin changes of one space are delivered in outbox commit order on the shared chat message endpoint (partitioned by space); the frame is notify-only and readers refetch the pins.")]
public sealed class ChatSpaceChangedRealtimeConsumer : IConsumer<ChatSpaceChangedIntegrationEvent>
{
    private readonly IRealtimePublisher _publisher;

    public ChatSpaceChangedRealtimeConsumer(IRealtimePublisher publisher) => _publisher = publisher;

    public Task Consume(ConsumeContext<ChatSpaceChangedIntegrationEvent> context) =>
        _publisher.PublishAsync(ChatRealtime.ForSpaceChanged(context.Message, DateTimeOffset.UtcNow), context.CancellationToken);
}

public sealed class ChatSpaceChangedRealtimeConsumerDefinition : ConsumerDefinition<ChatSpaceChangedRealtimeConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatSpaceChangedRealtimeConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        consumerConfigurator.Message<ChatSpaceChangedIntegrationEvent>(m =>
            m.UsePartitioner(ChatMessageRealtimePartitioning.PartitionCount, c => ChatMessageRealtimePartitioning.Key(c.Message.SpaceId)));
}
