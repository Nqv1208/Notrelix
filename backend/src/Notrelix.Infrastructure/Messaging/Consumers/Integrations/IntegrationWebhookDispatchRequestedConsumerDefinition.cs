namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

public sealed class IntegrationWebhookDispatchRequestedConsumerDefinition
    : ConsumerDefinition<IntegrationWebhookDispatchRequestedConsumer>
{
    public IntegrationWebhookDispatchRequestedConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-webhook-dispatch-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<IntegrationWebhookDispatchRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Intervals(WebhookRedeliveryPolicy.RedeliveryIntervals.ToArray());
            r.Ignore<ArgumentException>();
        });
    }
}
