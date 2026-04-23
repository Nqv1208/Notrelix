using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Document;

public class PagePublishedEvent : BaseEvent
{
    public Guid PageId { get; }
    public Guid WorkspaceId { get; }
    public Guid PublishedBy { get; }

    public PagePublishedEvent(Guid pageId, Guid workspaceId, Guid publishedBy)
    {
        PageId = pageId;
        WorkspaceId = workspaceId;
        PublishedBy = publishedBy;
    }
}
