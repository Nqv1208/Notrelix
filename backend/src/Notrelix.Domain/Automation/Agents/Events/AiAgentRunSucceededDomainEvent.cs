namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunSucceededDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt);
