namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserCreated;

public sealed class UserRegisteredConsumerDefinition : ConsumerDefinition<UserRegisteredConsumer>
{
    public UserRegisteredConsumerDefinition()
    {
        EndpointName = "notrelix-identity-user-registered-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UserRegisteredConsumer> consumerConfigurator,
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
