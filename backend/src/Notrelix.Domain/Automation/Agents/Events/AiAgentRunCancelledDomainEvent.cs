namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
