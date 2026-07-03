namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserRegistered;

public sealed class SendWelcomeEmailConsumerDefinition : ConsumerDefinition<SendWelcomeEmailConsumer>
{
    public SendWelcomeEmailConsumerDefinition()
    {
        EndpointName = "notrelix-identity-welcome-email-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SendWelcomeEmailConsumer> consumerConfigurator,
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
