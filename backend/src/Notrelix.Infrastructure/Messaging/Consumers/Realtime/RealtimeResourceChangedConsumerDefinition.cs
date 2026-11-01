namespace Notrelix.Infrastructure.Messaging.Consumers.Realtime;

/// <summary>
/// The outbox dispatcher hands a stream's messages to the bus back to back, in stream version order. This
/// partitions the consumer by stream key so frames of one stream are published one at a time in that order,
/// while different streams still run in parallel.
/// </summary>
public sealed class RealtimeResourceChangedConsumerDefinition : ConsumerDefinition<RealtimeResourceChangedConsumer>
{
    private const int PartitionCount = 64;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RealtimeResourceChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        consumerConfigurator.Message<RealtimeResourceChangedV1>(m =>
            m.UsePartitioner(PartitionCount, c => c.Message.StreamKey));
    }
}
