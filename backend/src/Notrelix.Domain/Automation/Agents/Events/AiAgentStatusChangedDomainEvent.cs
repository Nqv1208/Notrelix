namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentStatusChangedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AgentId { get; }
    public AiAgentStatus Status { get; }

    public AiAgentStatusChangedDomainEvent(
        Guid accountId, Guid workspaceId, Guid agentId, AiAgentStatus status,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        AgentId = agentId;
        Status = status;
    }
}
