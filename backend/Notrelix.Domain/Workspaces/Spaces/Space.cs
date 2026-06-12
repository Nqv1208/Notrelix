using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Spaces.Events;

namespace Notrelix.Domain.Workspaces.Spaces;

public class Space : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpaceVisibility Visibility { get; private set; }
    public SpaceStatus Status { get; private set; }

    private Space() : base() { }

    public static Space Create(Guid workspaceId, string name, SpaceVisibility visibility, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotEmpty(createdBy);

        var space = new Space
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Visibility = visibility,
            Status = SpaceStatus.Active
        };

        space.SetAuditOnCreate(createdBy, createdAt);
        space.AddDomainEvent(new SpaceCreatedEvent(space.Id, workspaceId, space.Name, createdBy, createdAt));

        return space;
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException("Cannot rename an archived space.");
        Guard.NotNullOrWhiteSpace(newName);
        Guard.NotEmpty(updatedBy);

        var oldName = Name;
        var normalizedName = newName.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new SpaceRenamedEvent(WorkspaceId, Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Move(Guid newWorkspaceId, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        if (Status == SpaceStatus.Archived)
            throw new BusinessRuleException("Cannot move an archived space.");
        Guard.NotEmpty(newWorkspaceId);
        Guard.NotEmpty(movedBy);

        if (WorkspaceId != newWorkspaceId)
        {
            throw new BusinessRuleException("Moving a space across workspaces is not allowed to maintain workspace isolation boundaries.");
        }
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == SpaceStatus.Archived) return;

        Status = SpaceStatus.Archived;
        SetAuditOnUpdate(archivedBy, archivedAt);
        AddDomainEvent(new SpaceArchivedEvent(WorkspaceId, Id, archivedBy, archivedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        Status = SpaceStatus.SoftDeleted;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new SpaceSoftDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        Status = SpaceStatus.Active;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new SpaceRestoredEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
