namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces.InvitationDelivery;

public sealed class SendInvitationEmailConsumerDefinition : ConsumerDefinition<SendInvitationEmailConsumer>
{
    public SendInvitationEmailConsumerDefinition()
    {
        EndpointName = "notrelix-workspaces-invitation-delivery-email-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SendInvitationEmailConsumer> consumerConfigurator,
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
