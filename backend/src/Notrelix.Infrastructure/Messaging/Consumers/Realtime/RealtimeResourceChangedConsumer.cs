namespace Notrelix.Infrastructure.Messaging.Consumers.Realtime;

public sealed class RealtimeResourceChangedConsumer : IConsumer<RealtimeResourceChangedV1>
{
    private readonly IRealtimePublisher _publisher;

    public RealtimeResourceChangedConsumer(IRealtimePublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Consume(ConsumeContext<RealtimeResourceChangedV1> context) =>
        _publisher.PublishAsync(context.Message, context.CancellationToken);
}
