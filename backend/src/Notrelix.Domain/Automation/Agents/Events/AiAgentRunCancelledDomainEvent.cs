namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-run-cancelled")]
public sealed record AiAgentRunCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
