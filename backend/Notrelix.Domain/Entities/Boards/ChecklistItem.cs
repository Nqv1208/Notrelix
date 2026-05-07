using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities.Boards;

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

    public static ChecklistItem Create(Guid checklistId, string title, double position = 0, Guid? assigneeId = null)
    {
        return new ChecklistItem
        {
            ChecklistId = checklistId,
            Title = title.Trim(),
            Position = position,
            AssigneeId = assigneeId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTitle(string title) => Title = title.Trim();
    public void Toggle() => IsChecked = !IsChecked;
    public void Check() => IsChecked = true;
    public void Uncheck() => IsChecked = false;
    public void SetDueDate(DateTime? dueDate) => DueDate = dueDate;
    public void Assign(Guid? assigneeId) => AssigneeId = assigneeId;
    public void UpdatePosition(double position) => Position = position;
}
