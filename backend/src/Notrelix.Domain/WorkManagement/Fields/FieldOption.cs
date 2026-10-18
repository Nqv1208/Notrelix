namespace Notrelix.Domain.WorkManagement.Fields;

public class FieldOption : Entity
{
    public Guid FieldId { get; private set; }
    public string Name { get; private set; } = null!;
    public Color Color { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    /// <summary>
    /// Work-status category of an option of a Status field; null for options of
    /// every other field type. The owning <see cref="BoardField"/> enforces both.
    /// </summary>
    public WorkStatusCategory? Category { get; private set; }

    private FieldOption() : base() { }

    public static FieldOption Create(Guid fieldId, string name, Color color, FractionalIndex position, WorkStatusCategory? category = null)
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
            Position = position,
            Category = category
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

    public void UpdateCategory(WorkStatusCategory? category)
    {
        Category = category;
    }
}
