namespace Notrelix.Domain.Automation.Agents.Events;

public sealed record AiAgentCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AgentId { get; }
    public string Name { get; }

    public AiAgentCreatedDomainEvent(
        Guid accountId, Guid workspaceId, Guid agentId, string name,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        AgentId = agentId;
        Name = name;
    }
}
