namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunStartedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
