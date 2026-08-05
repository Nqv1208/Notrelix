using Notrelix.Domain.WorkManagement.Fields;
namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemValue : Entity
{
    public Guid ItemId { get; private set; }
    public Guid FieldId { get; private set; }
    public FieldValue Value { get; private set; } = null!;

    private BoardItemValue() : base() { }

    public static BoardItemValue Create(Guid itemId, Guid fieldId, FieldValue value)
    {
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(fieldId);
        Guard.NotNull(value);

        return new BoardItemValue
        {
            ItemId = itemId,
            FieldId = fieldId,
            Value = value
        };
    }

    public void UpdateValue(FieldValue value)
    {
        Guard.NotNull(value);
        Value = value;
    }
}
