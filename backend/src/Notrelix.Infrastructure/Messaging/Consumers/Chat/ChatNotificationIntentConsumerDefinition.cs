namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

public sealed class ChatNotificationIntentConsumerDefinition : ConsumerDefinition<ChatNotificationIntentConsumer>
{
    public ChatNotificationIntentConsumerDefinition()
    {
        EndpointName = "notrelix-chat-notification-intent-v1";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatNotificationIntentConsumer> consumerConfigurator,
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
