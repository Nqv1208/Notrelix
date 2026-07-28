using Notrelix.Domain.Workspaces.Spaces.Events;
namespace Notrelix.Domain.Workspaces.Spaces;

public class Space : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpaceVisibility Visibility { get; private set; }
    public SpaceStatus Status { get; private set; }
    public SpaceType SpaceType { get; private set; } = SpaceType.Folder;

    private Space() : base() { }

    public static Space Create(
        Guid accountId,
        Guid workspaceId,
        string name,
        SpaceVisibility visibility,
        Guid createdBy,
        DateTimeOffset createdAt,
        SpaceType spaceType = SpaceType.Folder,
        string? description = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotEmpty(createdBy);

        var space = new Space
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Visibility = visibility,
            Status = SpaceStatus.Active,
            SpaceType = spaceType
        };

        space.SetAuditOnCreate(createdBy, createdAt);
        space.RaiseDomainEvent(new SpaceCreatedDomainEvent(space.Id, accountId, workspaceId, space.Name, createdBy, createdAt));

        return space;
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotRenameArchived, "Cannot rename an archived space.");

        var oldName = Name;
        var normalizedName = newName.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void UpdateDescription(string? newDescription, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotUpdateDescriptionArchived, "Cannot update description of an archived space.");

        var normalized = string.IsNullOrWhiteSpace(newDescription)
            ? null
            : newDescription.Trim();

        if (normalized is not null)
            Guard.MaxLength(normalized, 1024);

        if (Description == normalized) return;

        var oldDescription = Description;
        Description = normalized;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceDescriptionUpdatedDomainEvent(
            AccountId, WorkspaceId, Id, oldDescription, Description, updatedBy, updatedAt));
    }

    public void ChangeVisibility(SpaceVisibility newVisibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotChangeVisibilityArchived, "Cannot change visibility of an archived space.");

        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceVisibilityChangedDomainEvent(
            AccountId, WorkspaceId, Id, oldVisibility, newVisibility, updatedBy, updatedAt));
    }

    public void ChangeType(SpaceType newType, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);

        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Space_CannotChangeTypeArchived, "Cannot change type of an archived space.");

        if (SpaceType == newType) return;

        var oldType = SpaceType;
        SpaceType = newType;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceTypeChangedDomainEvent(
            AccountId, WorkspaceId, Id, oldType, newType, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);

        var audit = PrepareAuditUpdate(archivedBy, archivedAt);

        if (Status == SpaceStatus.Archived) return;

        Status = SpaceStatus.Archived;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);

        var audit = PrepareAuditUpdate(unarchivedBy, unarchivedAt);

        if (Status == SpaceStatus.Active) return;

        if (Status != SpaceStatus.Archived)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotUnarchiveNonArchived,
                "Only an archived space can be unarchived.");

        Status = SpaceStatus.Active;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceUnarchivedDomainEvent(
            AccountId, WorkspaceId, Id, unarchivedBy, unarchivedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        var audit = PrepareAuditUpdate(deletedBy, deletedAt);

        Status = SpaceStatus.SoftDeleted;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;

        var audit = PrepareAuditUpdate(restoredBy, restoredAt);

        Status = SpaceStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new SpaceRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
