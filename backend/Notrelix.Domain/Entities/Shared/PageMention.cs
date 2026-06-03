using Notrelix.Domain.Common;
using Notrelix.Domain.Events.Document;

namespace Notrelix.Domain.Entities.Shared;

/// <summary>
/// @mention user trong block → trigger notification
/// </summary>
public class PageMention : BaseEntity
{
    public Guid PageId { get; private set; }
    public Guid? BlockId { get; private set; }
    public Guid MentionedUserId { get; private set; }
    public Guid MentionedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PageMention() : base() { }

    public static PageMention Create(Guid pageId, Guid mentionedUserId, Guid mentionedBy, Guid? blockId = null)
    {
        var mention = new PageMention
        {
            PageId = pageId,
            BlockId = blockId,
            MentionedUserId = mentionedUserId,
            MentionedBy = mentionedBy,
            CreatedAt = DateTime.UtcNow
        };

        mention.AddDomainEvent(new PageMentionedEvent(pageId, blockId, mentionedUserId, mentionedBy));
        return mention;
    }
}
