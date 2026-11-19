namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

public sealed class ChatMessageRemovedNotificationConsumerDefinition : ConsumerDefinition<ChatMessageRemovedNotificationConsumer>
{
    public ChatMessageRemovedNotificationConsumerDefinition()
    {
        EndpointName = "notrelix-chat-message-removed-notification-v1";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatMessageRemovedNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 16;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
