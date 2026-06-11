using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Labels;

public sealed class LabelColor : ValueObject
{
    public string Hex { get; }

    private LabelColor(string hex)
    {
        Hex = hex;
    }

    public static LabelColor Create(string hex)
    {
        Guard.NotNullOrWhiteSpace(hex);
        return new LabelColor(hex.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hex;
    }
}

public class Label : AggregateRoot
{
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public LabelColor Color { get; private set; } = null!;
    public LabelStatus Status { get; private set; }

    private Label() : base() { }

    public static Label Create(Guid boardId, string name, LabelColor color, Guid createdBy)
    {
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(color);

        var label = new Label
        {
            BoardId = boardId,
            Name = name.Trim(),
            Color = color,
            Status = LabelStatus.Active
        };

        label.SetAuditOnCreate(createdBy);
        label.AddDomainEvent(new LabelCreatedEvent(boardId, label.Id, label.Name));

        return label;
    }

    public void Update(string name, LabelColor color, Guid updatedBy)
    {
        Name = name.Trim();
        Color = color;
        SetAuditOnUpdate(updatedBy);
        AddDomainEvent(new LabelUpdatedEvent(Id, updatedBy));
    }
}
