namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

public sealed class ChatWorkspaceMemberRemovedConsumerDefinition : ConsumerDefinition<ChatWorkspaceMemberRemovedConsumer>
{
    public ChatWorkspaceMemberRemovedConsumerDefinition()
    {
        EndpointName = "notrelix-chat-workspace-member-removed-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatWorkspaceMemberRemovedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
