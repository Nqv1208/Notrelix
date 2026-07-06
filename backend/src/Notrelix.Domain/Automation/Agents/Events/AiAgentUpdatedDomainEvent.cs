namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid AgentId,
    string Name,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ActorUserId);
