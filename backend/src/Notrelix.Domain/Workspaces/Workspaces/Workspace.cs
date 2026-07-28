using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Workspaces.Events;
namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed class Workspace :
    SoftDeletableAggregateRoot,
    IAccountScoped
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

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Workspace_CannotRenameArchived, "Cannot rename an archived workspace.");

        var oldName = Name;
        if (Name == newName.Trim()) return;

        Name = newName.Trim();
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRenamedDomainEvent(AccountId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);

        var audit = PrepareAuditUpdate(archivedBy, archivedAt);

        if (Status == WorkspaceStatus.Archived) return;

        Status = WorkspaceStatus.Archived;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceArchivedDomainEvent(AccountId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);

        var audit = PrepareAuditUpdate(unarchivedBy, unarchivedAt);

        if (Status == WorkspaceStatus.Active) return;

        if (Status != WorkspaceStatus.Archived)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Workspace_CannotUnarchiveNonArchived,
                "Only an archived workspace can be unarchived.");

        Status = WorkspaceStatus.Active;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceUnarchivedDomainEvent(AccountId, Id, unarchivedBy, unarchivedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        Status = WorkspaceStatus.SoftDeleted;
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceSoftDeletedDomainEvent(AccountId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        Status = WorkspaceStatus.Active;
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRestoredDomainEvent(AccountId, Id, restoredBy, restoredAt));
    }

    public void UpdateDescription(string? newDescription, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Workspace_CannotUpdateDescriptionArchived, "Cannot update description of an archived workspace.");

        var normalized = string.IsNullOrWhiteSpace(newDescription)
            ? null
            : newDescription.Trim();

        if (normalized is not null)
            Guard.MaxLength(normalized, 1024);

        if (Description == normalized)
            return;

        var oldDescription = Description;
        Description = normalized;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceDescriptionUpdatedDomainEvent(AccountId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void UpdateSettings(WorkspaceSettings newSettings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newSettings);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Workspace_CannotUpdateSettingsArchived, "Cannot update settings of an archived workspace.");

        if (Settings == newSettings) return;

        Settings = newSettings;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceSettingsUpdatedDomainEvent(AccountId, Id, updatedBy, updatedAt));
    }
}
