using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Checklists;

public class ChecklistItem : Entity
{
    public Guid ChecklistId { get; private set; }
    public string Title { get; private set; } = null!;
    public ChecklistItemStatus Status { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public double Position { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ChecklistItem() : base() { }

    public static ChecklistItem Create(Guid checklistId, string title, double position)
    {
        Guard.NotEmpty(checklistId);
        Guard.NotNullOrWhiteSpace(title);

        return new ChecklistItem
        {
            ChecklistId = checklistId,
            Title = title.Trim(),
            Status = ChecklistItemStatus.Open,
            Position = position
        };
    }

    public void Toggle()
    {
        Status = Status == ChecklistItemStatus.Open ? ChecklistItemStatus.Done : ChecklistItemStatus.Open;
        CompletedAt = Status == ChecklistItemStatus.Done ? DateTimeOffset.UtcNow : null;
    }
}

public class Checklist : AggregateRoot
{
    public Guid ItemId { get; private set; }
    public string Title { get; private set; } = null!;
    public double Position { get; private set; }

    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() : base() { }

    public static Checklist Create(Guid itemId, string title, double position, Guid createdBy)
    {
        Guard.NotEmpty(itemId);
        Guard.NotNullOrWhiteSpace(title);

        var checklist = new Checklist
        {
            ItemId = itemId,
            Title = title.Trim(),
            Position = position
        };

        checklist.SetAuditOnCreate(createdBy);
        checklist.AddDomainEvent(new ChecklistCreatedEvent(itemId, checklist.Id, checklist.Title));

        return checklist;
    }

    public void AddItem(string title, double position)
    {
        var item = ChecklistItem.Create(Id, title, position);
        _items.Add(item);
        AddDomainEvent(new ChecklistItemAddedEvent(Id, item.Id, title));
    }
}
