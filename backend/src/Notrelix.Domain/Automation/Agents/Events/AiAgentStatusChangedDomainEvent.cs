namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentStatusChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AgentId,
    AiAgentStatus Status,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorUserId);
