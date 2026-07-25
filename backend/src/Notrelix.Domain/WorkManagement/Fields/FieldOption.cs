namespace Notrelix.Domain.WorkManagement.Fields;

public class FieldOption : Entity
{
    public Guid FieldId { get; private set; }
    public string Name { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    private FieldOption() : base() { }

    public static FieldOption Create(Guid fieldId, string name, Color color, FractionalIndex position)
    {
        Guard.NotEmpty(fieldId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);
        Guard.NotNull(position);

        return new FieldOption
        {
            FieldId = fieldId,
            Name = name.Trim(),
            Color = color,
            Position = position
        };
    }

    public void Update(string name, Color color)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);
        Name = name.Trim();
        Color = color;
    }

    public void UpdatePosition(FractionalIndex position)
    {
        Guard.NotNull(position);
        Position = position;
    }
}
