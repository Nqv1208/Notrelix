using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Workspaces.Events;

namespace Notrelix.Domain.Workspaces.Workspaces;

public class Workspace : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkspaceStatus Status { get; private set; }
    public WorkspaceSettings Settings { get; private set; } = null!;
    public bool IsPersonal { get; private set; }
    public Guid? AccountId { get; private set; }

    private Workspace() : base() { }

    public static Workspace Create(Guid ownerId, string name, string slug, DateTimeOffset createdAt, bool isPersonal = false, Guid? accountId = null)
    {
        Guard.NotEmpty(ownerId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(slug);

        var workspace = new Workspace
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Status = WorkspaceStatus.Active,
            Settings = WorkspaceSettings.Create(),
            IsPersonal = isPersonal,
            AccountId = accountId
        };

        workspace.SetAuditOnCreate(ownerId, createdAt);
        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.Slug, ownerId, createdAt));

        return workspace;
    }

    public void AssignToAccount(Guid accountId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(accountId);

        if (AccountId == accountId) return;

        AccountId = accountId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException("Cannot rename an archived workspace.");

        var oldName = Name;
        if (Name == newName.Trim()) return;

        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceRenamedEvent(Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();

        if (Status == WorkspaceStatus.Archived) return;

        Status = WorkspaceStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceArchivedEvent(Id, archivedBy, archivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = WorkspaceStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceSoftDeletedEvent(Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = WorkspaceStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceRestoredEvent(Id, restoredBy, restoredAt));
    }

    public void UpdateSettings(WorkspaceSettings newSettings, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newSettings);

        if (Status == WorkspaceStatus.Archived)
            throw new BusinessRuleException("Cannot update settings of an archived workspace.");

        Settings = newSettings;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }
}
