namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentRunSucceededDomainEvent(
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId);
