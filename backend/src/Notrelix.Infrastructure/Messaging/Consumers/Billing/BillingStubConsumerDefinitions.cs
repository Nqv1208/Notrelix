namespace Notrelix.Infrastructure.Messaging.Consumers.Billing;

public sealed class SubscriptionCanceledConsumerDefinition : ConsumerDefinition<SubscriptionCanceledConsumer>
{
    public SubscriptionCanceledConsumerDefinition()
    {
        EndpointName = "notrelix-billing-subscription-canceled-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SubscriptionCanceledConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
