using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Board;

/// <summary>
/// Liên kết giữa các card: "Card A blocks Card B", "relates to", "duplicate of"
/// </summary>
public class CardLink : BaseEntity
{
    public Guid SourceCardId { get; private set; }
    public Guid TargetCardId { get; private set; }
    public CardLinkType LinkType { get; private set; } = CardLinkType.RelatesTo;
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Card SourceCard { get; private set; } = null!;
    public Card TargetCard { get; private set; } = null!;

    private CardLink() : base() { }

    public static CardLink Create(Guid sourceCardId, Guid targetCardId, CardLinkType linkType, Guid? createdBy = null)
    {
        if (sourceCardId == targetCardId)
            throw new ArgumentException("Không thể liên kết card với chính nó");

        return new CardLink
        {
            SourceCardId = sourceCardId,
            TargetCardId = targetCardId,
            LinkType = linkType,
            CreatedByUserId = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
