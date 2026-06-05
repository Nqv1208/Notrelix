using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceUpdatedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid UpdatedBy { get; }
    public string Name { get; }
    public string? Slug { get; }

    public WorkspaceUpdatedEvent(Guid workspaceId, Guid updatedBy, string name, string? slug = null)
    {
        WorkspaceId = workspaceId;
        UpdatedBy = updatedBy;
        Name = name;
        Slug = slug;
    }
}
