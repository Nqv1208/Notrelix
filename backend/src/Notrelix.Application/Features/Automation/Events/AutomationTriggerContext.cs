namespace Notrelix.Application.Features.Automation.Events;

/// <summary>
/// Neutral trigger input for the Automation rule evaluator: the authoritative
/// facts extracted from a Work integration event by its thin consumer. The
/// evaluator matches rules on <see cref="TriggerType"/>; consumers never carry
/// rule/execution semantics themselves.
/// </summary>
public sealed record AutomationTriggerContext(
    Guid AccountId,
    Guid WorkspaceId,
    string TriggerType,
    Guid SourceEventId,
    Guid? ActorUserId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset OccurredAt);
