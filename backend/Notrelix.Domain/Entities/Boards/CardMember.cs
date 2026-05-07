namespace Notrelix.Domain.Entities.Boards;

// Composite PK: CardId + UserId
public class CardMember
{
    public Guid CardId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Guid? AssignedBy { get; private set; }

    public Card Card { get; private set; } = null!;

    private CardMember() { }

    public static CardMember Create(Guid cardId, Guid userId, Guid? assignedBy = null)
    {
        return new CardMember
        {
            CardId = cardId,
            UserId = userId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }
}
