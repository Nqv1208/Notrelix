namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardCreatedConsumerDefinition : ConsumerDefinition<BoardCreatedConsumer>
{
    public BoardCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
