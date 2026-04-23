using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Document;

public class PageDeadlineSetEvent : BaseEvent
{
    public Guid PageId { get; }
    public Guid WorkspaceId { get; }
    public DateTime? Deadline { get; }

    public PageDeadlineSetEvent(Guid pageId, Guid workspaceId, DateTime? deadline)
    {
        PageId = pageId;
        WorkspaceId = workspaceId;
        Deadline = deadline;
    }
}
