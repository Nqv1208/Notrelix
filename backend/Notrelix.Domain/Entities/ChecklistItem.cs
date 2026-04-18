using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities;

public class ChecklistItem : BaseEntity
{
    public Guid ChecklistId { get; private set; }
    public string Title { get; private set; } = null!;
    public bool IsChecked { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public double Position { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Checklist Checklist { get; private set; } = null!;

    private ChecklistItem() : base() { }

    public static ChecklistItem Create(Guid checklistId, string title, double position = 0)
    {
        return new ChecklistItem
        {
            ChecklistId = checklistId,
            Title = title.Trim(),
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }
}
