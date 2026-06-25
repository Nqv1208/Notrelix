namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentStatusChangedDomainEvent(
    Guid WorkspaceId,
    Guid AgentId,
    AiAgentStatus Status,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
