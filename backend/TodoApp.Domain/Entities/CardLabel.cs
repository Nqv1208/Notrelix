namespace TodoApp.Domain.Entities;

public class CardLabel
{
    public Guid CardId { get; private set; }
    public Guid LabelId { get; private set; }

    public Card Card { get; private set; } = null!;
    public Label Label { get; private set; } = null!;

    private CardLabel() { }

    public static CardLabel Create(Guid cardId, Guid labelId)
    {
        return new CardLabel
        {
            CardId = cardId,
            LabelId = labelId
        };
    }
}
