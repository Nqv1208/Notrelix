namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardItemFieldValueChangedConsumerDefinition : ConsumerDefinition<BoardItemFieldValueChangedConsumer>
{
    public BoardItemFieldValueChangedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-item-field-changed-v1";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemFieldValueChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 16;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(100));
            r.Ignore<ArgumentException>();
        });
    }
}
