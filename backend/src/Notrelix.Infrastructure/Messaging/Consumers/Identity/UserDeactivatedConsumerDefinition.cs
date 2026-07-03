namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class UserDeactivatedConsumerDefinition : ConsumerDefinition<UserDeactivatedConsumer>
{
    public UserDeactivatedConsumerDefinition()
    {
        EndpointName = "notrelix-identity-user-deactivated-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UserDeactivatedConsumer> consumerConfigurator,
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
