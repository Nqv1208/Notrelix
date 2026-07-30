using Notrelix.Domain.WorkManagement.Checklists.Events;
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

public class Checklist : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ItemId { get; private set; }
    public string Title { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;

    private readonly List<ChecklistItem> _items = new();
    public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

    private Checklist() : base() { }

    public static Checklist Create(Guid accountId, Guid workspaceId, Guid itemId, string title, FractionalIndex position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(itemId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);

        var checklist = new Checklist
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ItemId = itemId,
            Title = title.Trim(),
            Position = position
        };

        checklist.SetAuditOnCreate(createdBy, createdAt);
        checklist.RaiseDomainEvent(new ChecklistCreatedDomainEvent(accountId, workspaceId, itemId, checklist.Id, checklist.Title, createdAt));

        return checklist;
    }

    public void AddItem(string title, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(addedBy);
        Guard.NotNull(position);
        var pending = PrepareAuditUpdate(addedBy, addedAt);
        var item = ChecklistItem.Create(Id, title, position);
        _items.Add(item);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemAddedDomainEvent(AccountId, WorkspaceId, Id, item.Id, title, addedAt));
    }

    public void ToggleItem(Guid itemId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item == null) throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Checklist_ItemNotFound, $"Item {itemId} not found");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        item.Toggle(updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemToggledDomainEvent(AccountId, WorkspaceId, Id, item.Id, item.Status == ChecklistItemStatus.Done, updatedAt));
    }

    public void RemoveItem(Guid itemId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item == null) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _items.Remove(item);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemRemovedDomainEvent(AccountId, WorkspaceId, Id, item.Id, updatedAt));
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(title);
        var normalizedTitle = title.Trim();
        if (Title == normalizedTitle) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Title = normalizedTitle;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void UpdatePosition(FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(position);
        if (Position.Value == position.Value) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Position = position;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
