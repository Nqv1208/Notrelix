namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationNotice;

public sealed class SendRegistrationNoticeEmailConsumerDefinition
    : ConsumerDefinition<SendRegistrationNoticeEmailConsumer>
{
    public SendRegistrationNoticeEmailConsumerDefinition()
    {
        EndpointName = "notrelix-identity-registration-notice-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SendRegistrationNoticeEmailConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(500));
            r.Ignore<ArgumentException>();
        });
    }
}
