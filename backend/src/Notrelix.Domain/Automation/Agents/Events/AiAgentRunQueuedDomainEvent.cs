namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-run-queued")]
public sealed record AiAgentRunQueuedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
