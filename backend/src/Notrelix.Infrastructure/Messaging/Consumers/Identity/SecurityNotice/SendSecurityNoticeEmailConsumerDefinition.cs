namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.SecurityNotice;

public sealed class SendSecurityNoticeEmailConsumerDefinition
    : ConsumerDefinition<SendSecurityNoticeEmailConsumer>
{
    public SendSecurityNoticeEmailConsumerDefinition()
    {
        EndpointName = "notrelix-identity-security-notice-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SendSecurityNoticeEmailConsumer> consumerConfigurator,
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
