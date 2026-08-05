namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-run-started")]
public sealed record AiAgentRunStartedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
