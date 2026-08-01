using Notrelix.Domain.Governance.Templates.Events;
using static Notrelix.Domain.Governance.GovernanceRuleCodes;

namespace Notrelix.Domain.Governance.Templates;

public class PermissionTemplate : AggregateRoot
{
    public Guid? AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ResourceType? TargetResourceType { get; private set; }
    public PermissionTemplateDefinition Definition { get; private set; } = null!;
    public PermissionTemplateScope Scope { get; private set; }
    public PermissionTemplateStatus Status { get; private set; }

    private PermissionTemplate() : base() { }

    public static PermissionTemplate CreateSystem(
        string name,
        PermissionTemplateDefinition definition,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? description = null,
        ResourceType? targetResourceType = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(definition);

        var template = new PermissionTemplate
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            TargetResourceType = targetResourceType,
            Definition = definition,
            Scope = PermissionTemplateScope.System,
            Status = PermissionTemplateStatus.Active
        };

        template.SetAuditOnCreate(createdBy, createdAt);
        template.RaiseDomainEvent(new SystemPermissionTemplateCreatedDomainEvent(template.Id, template.Name, createdBy, createdAt));
        return template;
    }

    public static PermissionTemplate CreateWorkspace(
        Guid accountId,
        Guid workspaceId,
        string name,
        PermissionTemplateDefinition definition,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? description = null,
        ResourceType? targetResourceType = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(definition);

        var template = new PermissionTemplate
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            TargetResourceType = targetResourceType,
            Definition = definition,
            Scope = PermissionTemplateScope.Workspace,
            Status = PermissionTemplateStatus.Active
        };

        template.SetAuditOnCreate(createdBy, createdAt);
        template.RaiseDomainEvent(new WorkspacePermissionTemplateCreatedDomainEvent(
            accountId,
            workspaceId,
            template.Id,
            template.Name,
            createdBy,
            createdAt));
        return template;
    }

    public void Archive(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Scope == PermissionTemplateScope.System)
            throw new BusinessRuleException(Governance_PermissionTemplate_CannotModifySystem, "System templates cannot be modified.");

        if (Status == PermissionTemplateStatus.Archived) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = PermissionTemplateStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PermissionTemplateArchivedDomainEvent(AccountId!.Value, WorkspaceId!.Value, Id, updatedBy, updatedAt));
    }
}
