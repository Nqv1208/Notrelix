using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Checklists;

public class ChecklistItem : Entity
{
    public Guid ChecklistId { get; private set; }
    public string Title { get; private set; } = null!;
    public ChecklistItemStatus Status { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public FractionalIndex Position { get; private set; } = null!;
    public DateTimeOffset? CompletedAt { get; private set; }

    private ChecklistItem() : base() { }

    public static ChecklistItem Create(Guid checklistId, string title, FractionalIndex position)
    {
        Guard.NotEmpty(checklistId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(position);

        return new ChecklistItem
        {
            ChecklistId = checklistId,
            Title = title.Trim(),
            Position = position
        };
    }

    public void Toggle(DateTimeOffset toggledAt)
    {
        Status = Status == ChecklistItemStatus.Open ? ChecklistItemStatus.Done : ChecklistItemStatus.Open;
        CompletedAt = Status == ChecklistItemStatus.Done ? toggledAt : null;
    }
}

public class Checklist : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid ItemId { get; private set; }
    public string Title { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() : base() { }

    public static Checklist Create(Guid workspaceId, Guid itemId, string title, FractionalIndex position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(itemId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(position);

        var checklist = new Checklist
        {
            WorkspaceId = workspaceId,
            ItemId = itemId,
            Title = title.Trim(),
            Position = position
        };

        checklist.SetAuditOnCreate(createdBy, createdAt);
        checklist.AddDomainEvent(new ChecklistCreatedEvent(workspaceId, itemId, checklist.Id, checklist.Title, createdAt));

        return checklist;
    }

    public void AddItem(string title, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(position);
        var item = ChecklistItem.Create(Id, title, position);
        _items.Add(item);
        SetAuditOnUpdate(addedBy, addedAt);
        AddDomainEvent(new ChecklistItemAddedEvent(WorkspaceId, Id, item.Id, title, addedAt));
    }

    public void ToggleItem(Guid itemId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item == null) throw new BusinessRuleException($"Item {itemId} not found");

        item.Toggle(updatedAt);
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new ChecklistItemToggledEvent(WorkspaceId, Id, item.Id, item.Status == ChecklistItemStatus.Done, updatedAt));
    }

    public void RemoveItem(Guid itemId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item == null) return;

        _items.Remove(item);
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new ChecklistItemRemovedEvent(WorkspaceId, Id, item.Id, updatedAt));
    }
}
