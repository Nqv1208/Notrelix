namespace Notrelix.Domain.Automation.Agents.Events;

[EventName("automation.ai-agent-updated")]
public sealed record AiAgentUpdatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AgentId { get; }
    public string Name { get; }

    public AiAgentUpdatedDomainEvent(
        Guid accountId, Guid workspaceId, Guid agentId, string name,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        AgentId = agentId;
        Name = name;
    }
}
