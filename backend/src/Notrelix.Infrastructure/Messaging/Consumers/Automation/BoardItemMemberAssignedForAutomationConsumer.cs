using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Automation.Events;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Thin inbound adapter for the WorkManagement-owned member-assigned fact.
/// Automation subscribes to the producer-owned contract; the fact's ownership
/// never depended on this consumer's existence.
/// </summary>
public sealed class BoardItemMemberAssignedAutomationConsumer
    : IConsumer<BoardItemMemberAssignedIntegrationEvent>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemMemberAssignedAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemMemberAssignedIntegrationEvent> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}
