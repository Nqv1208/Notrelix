namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunFailedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt);
