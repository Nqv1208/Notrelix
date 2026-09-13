using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Automation.Events;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Thin inbound adapter for the WorkManagement-owned moved-item fact. Extracts
/// the authoritative facts and delegates to the Automation rule evaluator;
/// no rule/execution semantics live here.
/// </summary>
public sealed class BoardItemMovedAutomationConsumer
    : IConsumer<BoardItemMovedIntegrationEvent>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemMovedAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemMovedIntegrationEvent> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}

/// <summary>
/// Thin inbound adapter for the WorkManagement-owned created-item fact. Extracts
/// the authoritative facts and delegates to the Automation rule evaluator; no
/// rule/execution semantics live here.
/// </summary>
public sealed class BoardItemCreatedAutomationConsumer
    : IConsumer<BoardItemCreatedIntegrationEvent>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemCreatedAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemCreatedIntegrationEvent> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}
