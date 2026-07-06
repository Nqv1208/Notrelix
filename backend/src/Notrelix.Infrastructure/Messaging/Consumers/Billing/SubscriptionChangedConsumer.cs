using Notrelix.Application.Events.Billing;

namespace Notrelix.Infrastructure.Messaging.Consumers.Billing;

public sealed class SubscriptionChangedConsumer : IConsumer<SubscriptionChangedIntegrationEvent>
{
    private readonly ILogger<SubscriptionChangedConsumer> _logger;

    public SubscriptionChangedConsumer(ILogger<SubscriptionChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SubscriptionChangedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Billing] SubscriptionChanged: SubscriptionId={SubscriptionId}, PreviousPlanId={PreviousPlanId}, NewPlanId={NewPlanId}",
            context.Message.SubscriptionId,
            context.Message.PreviousPlanId,
            context.Message.NewPlanId);
        return Task.CompletedTask;
    }
}
