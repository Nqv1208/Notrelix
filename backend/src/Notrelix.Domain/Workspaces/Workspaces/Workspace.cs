namespace Notrelix.Domain.Workspaces.Workspaces;

public class Workspace : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkspaceStatus Status { get; private set; }
    public WorkspaceSettings Settings { get; private set; } = null!;
    public bool IsPersonal { get; private set; }
    public Guid AccountId { get; private set; }

    private Workspace() : base() { }

    public static Workspace Create(Guid accountId, Guid ownerId, string name, string slug, DateTimeOffset createdAt, string? description = null, bool isPersonal = false)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(ownerId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(slug);
        Guard.MaxLength(name, 160);

        var slugValue = SharedKernel.Slug.Create(slug);

        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        if (normalizedDescription is not null)
            Guard.MaxLength(normalizedDescription, 1024);

        var workspace = new Workspace
        {
            AccountId = accountId,
            Name = name.Trim(),
            Slug = slugValue.Value,
            Description = normalizedDescription,
            Status = WorkspaceStatus.Active,
            Settings = WorkspaceSettings.Create(),
            IsPersonal = isPersonal
        };

        workspace.SetAuditOnCreate(ownerId, createdAt);
        workspace.RaiseDomainEvent(new WorkspaceCreatedDomainEvent(workspace.AccountId, workspace.Id, workspace.Name, workspace.Slug, ownerId, createdAt));

        return workspace;
    }

    public void UpdateAccountId(Guid newAccountId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(newAccountId);

        if (AccountId == newAccountId) return;

        AccountId = newAccountId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(BusinessRuleCodes.Workspaces_Workspace_CannotRenameArchived, "Cannot rename an archived workspace.");

        var oldName = Name;
        if (Name == newName.Trim()) return;

        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRenamedDomainEvent(Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();

        if (Status == WorkspaceStatus.Archived) return;

        Status = WorkspaceStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceArchivedDomainEvent(Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);

        if (Status == WorkspaceStatus.Active) return;

        if (Status != WorkspaceStatus.Archived)
            throw new BusinessRuleException(
                "Only an archived workspace can be unarchived.");

        Status = WorkspaceStatus.Active;
        SetAuditOnUpdate(unarchivedBy, unarchivedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceUnarchivedDomainEvent(Id, unarchivedBy, unarchivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = WorkspaceStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceSoftDeletedDomainEvent(Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = WorkspaceStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRestoredDomainEvent(Id, restoredBy, restoredAt));
    }

    public void UpdateDescription(string? newDescription, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(BusinessRuleCodes.Workspaces_Workspace_CannotUpdateDescriptionArchived, "Cannot update description of an archived workspace.");

        var normalized = string.IsNullOrWhiteSpace(newDescription)
            ? null
            : newDescription.Trim();

        if (normalized is not null)
            Guard.MaxLength(normalized, 1024);

        if (Description == normalized)
            return;

        var oldDescription = Description;
        Description = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceDescriptionUpdatedDomainEvent(Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void UpdateSettings(WorkspaceSettings newSettings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newSettings);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(BusinessRuleCodes.Workspaces_Workspace_CannotUpdateSettingsArchived, "Cannot update settings of an archived workspace.");

        if (Settings == newSettings) return;

        Settings = newSettings;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceSettingsUpdatedDomainEvent(Id, updatedBy, updatedAt));
    }
}
