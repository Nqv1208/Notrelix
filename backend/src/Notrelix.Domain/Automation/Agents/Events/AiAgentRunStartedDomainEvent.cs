namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunStartedDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt);
