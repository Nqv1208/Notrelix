namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunCancelledDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorUserId);
