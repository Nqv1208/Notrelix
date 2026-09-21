namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

public sealed class CalendarWebhookProcessingRequestedConsumerDefinition
    : ConsumerDefinition<CalendarWebhookProcessingRequestedConsumer>
{
    public CalendarWebhookProcessingRequestedConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-calendar-webhook-processing-requested-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CalendarWebhookProcessingRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
            r.Ignore<CalendarWebhookReceiptNotFoundException>();
        });
    }
}
