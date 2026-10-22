using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Automation.Events;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Thin inbound adapter for the WorkManagement-owned moved-item fact. Extracts
/// the authoritative facts and delegates to the Automation rule evaluator;
/// no rule/execution semantics live here.
/// </summary>
[OrderingNotApplicable("Each move fact independently triggers rule evaluation; evaluation is per message and deduplicated by the Platform dedup filter, with no later-supersedes-earlier state.")]
public sealed class BoardItemMovedAutomationConsumer
    : IConsumer<BoardItemMovedIntegrationEventV2>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemMovedAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemMovedIntegrationEventV2> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}

/// <summary>
/// Thin inbound adapter for the WorkManagement-owned created-item fact. Extracts
/// the authoritative facts and delegates to the Automation rule evaluator; no
/// rule/execution semantics live here.
/// </summary>
[OrderingNotApplicable("Each created fact independently triggers rule evaluation; evaluation is per message and deduplicated by the Platform dedup filter, with no later-supersedes-earlier state.")]
public sealed class BoardItemCreatedAutomationConsumer
    : IConsumer<BoardItemCreatedIntegrationEventV2>
{
    private readonly N8nAutomationRuleEvaluator _evaluator;

    public BoardItemCreatedAutomationConsumer(N8nAutomationRuleEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task Consume(ConsumeContext<BoardItemCreatedIntegrationEventV2> context) =>
        _evaluator.ExecuteAsync(context.Message, context.CancellationToken);
}
