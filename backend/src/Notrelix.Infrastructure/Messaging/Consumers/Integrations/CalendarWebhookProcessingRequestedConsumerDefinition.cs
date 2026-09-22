using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

public sealed class CalendarWebhookProcessingRequestedConsumerDefinition
    : ConsumerDefinition<CalendarWebhookProcessingRequestedConsumer>
{
    public CalendarWebhookProcessingRequestedConsumerDefinition(
        IOptions<MessagingEndpointOptions> options)
    {
        EndpointName = MessagingEndpointNames.CalendarWebhookProcessingRequested(
            options.Value.Transport,
            options.Value.EndpointPrefix);
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
