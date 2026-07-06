namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardItemCreatedConsumerDefinition : ConsumerDefinition<BoardItemCreatedConsumer>
{
    public BoardItemCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-item-created-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
