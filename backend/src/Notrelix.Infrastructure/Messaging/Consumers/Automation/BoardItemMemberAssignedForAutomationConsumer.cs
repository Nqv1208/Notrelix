using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Events;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

public sealed class BoardItemMemberAssignedForAutomationConsumer
    : IConsumer<BoardItemMemberAssignedForAutomationIntegrationEvent>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemMemberAssignedForAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemMemberAssignedForAutomationIntegrationEvent> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}
