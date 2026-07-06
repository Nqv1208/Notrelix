namespace Notrelix.Domain.Workspaces.Spaces;

public class Space : AggregateRoot, IWorkspaceScoped
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
        space.AddDomainEvent(new SpaceCreatedDomainEvent(space.Id, accountId, workspaceId, space.Name, createdBy, createdAt));

        return space;
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException("Cannot rename an archived space.");
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);
        Guard.NotEmpty(updatedBy);

        var oldName = Name;
        var normalizedName = newName.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new SpaceRenamedDomainEvent(AccountId, WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Move(Guid newWorkspaceId, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException("Cannot move an archived space.");
        Guard.NotEmpty(newWorkspaceId);
        Guard.NotEmpty(movedBy);

        if (WorkspaceId != newWorkspaceId)
            throw new BusinessRuleException("Moving a space across workspaces is not allowed.");
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == SpaceStatus.Archived) return;

        Status = SpaceStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        AddDomainEvent(new SpaceArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        Status = SpaceStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new SpaceSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        Status = SpaceStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new SpaceRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
