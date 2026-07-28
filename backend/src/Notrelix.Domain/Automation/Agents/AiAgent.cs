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

public class AiAgent : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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
        Guid accountId,
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
        Guard.NotEmpty(accountId);
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
            AccountId = accountId,
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
        agent.RaiseDomainEvent(new AiAgentCreatedDomainEvent(accountId, workspaceId, agent.Id, name, createdAt));
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

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = name.Trim();
        Description = description?.Trim();
        ModelPolicy = modelPolicy;
        Instruction = instruction;
        ToolPermissions = toolPermissions;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentUpdatedDomainEvent(AccountId, WorkspaceId, Id, Name, updatedAt));
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

        var validTransitions = Status switch
        {
            AiAgentStatus.Draft => new[] { AiAgentStatus.Enabled },
            AiAgentStatus.Enabled => new[] { AiAgentStatus.Paused, AiAgentStatus.Disabled },
            AiAgentStatus.Paused => new[] { AiAgentStatus.Enabled, AiAgentStatus.Disabled },
            AiAgentStatus.Disabled => new[] { AiAgentStatus.Enabled },
            _ => Array.Empty<AiAgentStatus>()
        };

        if (!validTransitions.Contains(newStatus))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Agent_InvalidStatusTransition, $"Cannot transition from {Status} to {newStatus}.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = newStatus;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentStatusChangedDomainEvent(AccountId, WorkspaceId, Id, Status, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (Status == AiAgentStatus.Deleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        Status = AiAgentStatus.Deleted;
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentStatusChangedDomainEvent(AccountId, WorkspaceId, Id, Status, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (Status != AiAgentStatus.Deleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        Status = AiAgentStatus.Draft;
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentStatusChangedDomainEvent(AccountId, WorkspaceId, Id, Status, restoredAt));
    }
}
