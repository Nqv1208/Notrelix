namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-run-succeeded")]
public sealed record AiAgentRunSucceededDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
