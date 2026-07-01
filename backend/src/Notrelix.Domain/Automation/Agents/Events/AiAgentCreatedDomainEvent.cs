namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AgentId,
    string Name,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorUserId);
