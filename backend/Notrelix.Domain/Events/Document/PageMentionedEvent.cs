using Notrelix.Domain.Common;

namespace Notrelix.Domain.Events.Document;

public class PageMentionedEvent : BaseEvent
{
    public Guid PageId { get; }
    public Guid? BlockId { get; }
    public Guid MentionedUserId { get; }
    public Guid MentionedBy { get; }

    public PageMentionedEvent(Guid pageId, Guid? blockId, Guid mentionedUserId, Guid mentionedBy)
    {
        PageId = pageId;
        BlockId = blockId;
        MentionedUserId = mentionedUserId;
        MentionedBy = mentionedBy;
    }
}
