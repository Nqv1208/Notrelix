namespace Notrelix.Infrastructure.Messaging.Consumers.Realtime;

[OrderingNotApplicable("Notify-only fan-out: the gateway allocates the per-stream version at publish and clients reconcile gaps and staleness by refetching authoritative state, so arrival order cannot corrupt state.")]
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
