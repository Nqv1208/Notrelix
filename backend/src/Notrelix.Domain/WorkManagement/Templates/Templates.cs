using Notrelix.Domain.WorkManagement.Templates.Events;
namespace Notrelix.Domain.WorkManagement.Templates;

public class BoardTemplate : SoftDeletableAggregateRoot
{
    public Guid? WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public JsonValue Structure { get; private set; } = null!;
    public TemplateStatus Status { get; private set; }

    private BoardTemplate() : base() { }

    public static BoardTemplate Create(string name, JsonValue structure, DateTimeOffset createdAt, Guid? workspaceId = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(structure);

        var template = new BoardTemplate
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Structure = structure,
            Status = TemplateStatus.Published
        };

        template.SetAuditOnCreate(null, createdAt);
        template.RaiseDomainEvent(new BoardTemplateCreatedDomainEvent(template.Id, template.Name, createdAt));
        return template;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Draft(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == TemplateStatus.Draft) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CannotDraftArchived, "Cannot draft an archived template. Restore it first.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = TemplateStatus.Draft;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == TemplateStatus.Published) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CannotPublishArchived, "Cannot publish an archived template. Restore it first.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = TemplateStatus.Published;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == TemplateStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = TemplateStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted && Status != TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CanOnlyRestoreArchived, "Only archived or deleted templates can be restored.");

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        Status = TemplateStatus.Draft;
        IncrementVersion();
    }
}

public class ItemTemplate : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public JsonValue Values { get; private set; } = null!;
    public TemplateStatus Status { get; private set; }

    private ItemTemplate() : base() { }

    public static ItemTemplate Create(Guid accountId, Guid workspaceId, Guid boardId, string name, JsonValue values, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(values);
        Guard.NotEmpty(accountId);

        var template = new ItemTemplate
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Values = values,
            Status = TemplateStatus.Published
        };

        template.SetAuditOnCreate(null, createdAt);
        template.RaiseDomainEvent(new ItemTemplateCreatedDomainEvent(accountId, workspaceId, template.Id, template.Name, createdAt));
        return template;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 255);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Draft(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == TemplateStatus.Draft) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CannotDraftArchived, "Cannot draft an archived template. Restore it first.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = TemplateStatus.Draft;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == TemplateStatus.Published) return;
        if (Status == TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CannotPublishArchived, "Cannot publish an archived template. Restore it first.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = TemplateStatus.Published;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == TemplateStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = TemplateStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted && Status != TemplateStatus.Archived)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardTemplate_CanOnlyRestoreArchived, "Only archived or deleted templates can be restored.");

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        Status = TemplateStatus.Draft;
        IncrementVersion();
    }
}
