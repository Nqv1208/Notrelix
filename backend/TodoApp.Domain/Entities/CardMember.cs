namespace TodoApp.Domain.Entities;

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
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };
    }
}
