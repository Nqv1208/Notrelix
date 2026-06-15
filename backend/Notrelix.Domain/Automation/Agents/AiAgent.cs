using System;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Automation.Agents.Events;

namespace Notrelix.Domain.Automation.Agents;

public enum AiAgentScopeType
{
    Workspace,
    Board,
    Doc,
    Dashboard
}

public enum AiAgentStatus
{
    Draft,
    Enabled,
    Paused,
    Disabled,
    Deleted
}

public class AiAgent : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AiAgentScopeType ScopeType { get; private set; }
    public Guid? ScopeResourceId { get; private set; }
    public AiAgentStatus Status { get; private set; }
    public JsonValue ModelPolicy { get; private set; } = null!;
    public JsonValue Instruction { get; private set; } = null!;
    public JsonValue ToolPermissions { get; private set; } = null!;

    private AiAgent() : base() { }

    public static AiAgent Create(
        Guid workspaceId,
        string name,
        string? description,
        AiAgentScopeType scopeType,
        Guid? scopeResourceId,
        JsonValue modelPolicy,
        JsonValue instruction,
        JsonValue toolPermissions,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(modelPolicy);
        Guard.NotNull(instruction);
        Guard.NotNull(toolPermissions);

        if (scopeType != AiAgentScopeType.Workspace)
        {
            Guard.NotEmpty(scopeResourceId ?? Guid.Empty, nameof(scopeResourceId));
        }

        var agent = new AiAgent
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            ScopeType = scopeType,
            ScopeResourceId = scopeResourceId,
            Status = AiAgentStatus.Draft,
            ModelPolicy = modelPolicy,
            Instruction = instruction,
            ToolPermissions = toolPermissions
        };

        agent.SetAuditOnCreate(createdBy, createdAt);
        agent.AddDomainEvent(new AiAgentCreatedEvent(workspaceId, agent.Id, name, createdBy, createdAt));
        return agent;
    }

    public void Update(
        string name,
        string? description,
        JsonValue modelPolicy,
        JsonValue instruction,
        JsonValue toolPermissions,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(modelPolicy);
        Guard.NotNull(instruction);
        Guard.NotNull(toolPermissions);

        Name = name.Trim();
        Description = description?.Trim();
        ModelPolicy = modelPolicy;
        Instruction = instruction;
        ToolPermissions = toolPermissions;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AiAgentUpdatedEvent(WorkspaceId, Id, Name, updatedBy, updatedAt));
    }

    public void ChangeStatus(AiAgentStatus newStatus, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == newStatus) return;

        if (newStatus == AiAgentStatus.Deleted)
        {
            SoftDelete(updatedBy, updatedAt);
            return;
        }

        Status = newStatus;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new AiAgentStatusChangedEvent(WorkspaceId, Id, Status, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (Status == AiAgentStatus.Deleted) return;
        Status = AiAgentStatus.Deleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new AiAgentStatusChangedEvent(WorkspaceId, Id, Status, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (Status != AiAgentStatus.Deleted) return;
        Status = AiAgentStatus.Draft;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new AiAgentStatusChangedEvent(WorkspaceId, Id, Status, restoredBy, restoredAt));
    }
}
