namespace Notrelix.Infrastructure.Messaging.Consumers.Documents;

public sealed class PageCreatedConsumerDefinition : ConsumerDefinition<PageCreatedConsumer>
{
    public PageCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-doc-page-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PageCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class PageArchivedConsumerDefinition : ConsumerDefinition<PageArchivedConsumer>
{
    public PageArchivedConsumerDefinition()
    {
        EndpointName = "notrelix-doc-page-archived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PageArchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
