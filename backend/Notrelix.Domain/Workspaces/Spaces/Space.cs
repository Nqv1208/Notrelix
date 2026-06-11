using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Spaces;

public class Space : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpaceVisibility Visibility { get; private set; }
    public SpaceStatus Status { get; private set; }

    private Space() : base() { }

    public static Space Create(Guid workspaceId, string name, SpaceVisibility visibility, Guid createdBy)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var space = new Space
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Visibility = visibility,
            Status = SpaceStatus.Active
        };

        space.SetAuditOnCreate(createdBy);
        space.AddDomainEvent(new SpaceCreatedEvent(space.Id, workspaceId, space.Name, createdBy));

        return space;
    }

    public void Rename(string newName, Guid updatedBy)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);

        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy);
    }

    public void Move(Guid newWorkspaceId, Guid movedBy)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(newWorkspaceId);

        var oldWorkspaceId = WorkspaceId;
        if (WorkspaceId == newWorkspaceId) return;

        WorkspaceId = newWorkspaceId;
        SetAuditOnUpdate(movedBy);
        AddDomainEvent(new SpaceMovedEvent(Id, oldWorkspaceId, newWorkspaceId, movedBy));
    }
}
