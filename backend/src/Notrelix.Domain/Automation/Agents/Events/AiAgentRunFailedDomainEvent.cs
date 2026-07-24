namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-run-failed")]
public sealed record AiAgentRunFailedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RunId,
    Guid AgentId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
