namespace Notrelix.Infrastructure.Messaging.Consumers.Accounts;

public sealed class AccountCreatedConsumerDefinition : ConsumerDefinition<AccountCreatedConsumer>
{
    public AccountCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-account-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AccountCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
