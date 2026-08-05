using Notrelix.Application.Events.Billing;

namespace Notrelix.Infrastructure.Messaging.Consumers.Billing;

public sealed class SubscriptionCanceledConsumer : IConsumer<SubscriptionCanceledIntegrationEvent>
{
    private readonly ILogger<SubscriptionCanceledConsumer> _logger;

    public SubscriptionCanceledConsumer(ILogger<SubscriptionCanceledConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SubscriptionCanceledIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Billing] SubscriptionCanceled: SubscriptionId={SubscriptionId}, WorkspaceId={WorkspaceId}, EffectiveAt={EffectiveAt}",
            context.Message.SubscriptionId,
            context.Message.WorkspaceId,
            context.Message.EffectiveAt);
        return Task.CompletedTask;
    }
}
