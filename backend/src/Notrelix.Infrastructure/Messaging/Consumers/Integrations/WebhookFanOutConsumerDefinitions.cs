namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

public sealed class BoardItemCreatedWebhookFanOutConsumerDefinition : ConsumerDefinition<BoardItemCreatedWebhookFanOutConsumer>
{
    public BoardItemCreatedWebhookFanOutConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-webhook-fanout-item-created-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemCreatedWebhookFanOutConsumer> consumerConfigurator,
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

public sealed class BoardItemRenamedWebhookFanOutConsumerDefinition : ConsumerDefinition<BoardItemRenamedWebhookFanOutConsumer>
{
    public BoardItemRenamedWebhookFanOutConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-webhook-fanout-item-renamed-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemRenamedWebhookFanOutConsumer> consumerConfigurator,
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

public sealed class BoardItemFieldChangedWebhookFanOutConsumerDefinition : ConsumerDefinition<BoardItemFieldChangedWebhookFanOutConsumer>
{
    public BoardItemFieldChangedWebhookFanOutConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-webhook-fanout-item-field-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemFieldChangedWebhookFanOutConsumer> consumerConfigurator,
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

public sealed class CommentCreatedWebhookFanOutConsumerDefinition : ConsumerDefinition<CommentCreatedWebhookFanOutConsumer>
{
    public CommentCreatedWebhookFanOutConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-webhook-fanout-comment-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CommentCreatedWebhookFanOutConsumer> consumerConfigurator,
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
