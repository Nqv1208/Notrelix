namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunFailedDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt);
