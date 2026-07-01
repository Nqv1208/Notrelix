namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunQueuedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt);
