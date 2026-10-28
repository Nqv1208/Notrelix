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

    public bool IsDone => Status == ChecklistItemStatus.Done;

    internal void Toggle(DateTimeOffset toggledAt) => SetCompletion(!IsDone, toggledAt);

    internal void SetCompletion(bool done, DateTimeOffset changedAt)
    {
        Status = done ? ChecklistItemStatus.Done : ChecklistItemStatus.Open;
        CompletedAt = done ? changedAt : null;
    }

    internal void Rename(string title) => Title = title;

    internal void UpdatePosition(FractionalIndex position) => Position = position;
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

    /// <summary>Maximum title length for a checklist and for one of its items.</summary>
    public const int MaxTitleLength = 200;

    private Checklist() : base() { }

    private static string NormalizeTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
        var normalized = title.Trim();
        Guard.MaxLength(normalized, MaxTitleLength);
        return normalized;
    }

    private ChecklistItem FindItem(Guid itemId) =>
        _items.FirstOrDefault(x => x.Id == itemId)
        ?? throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Checklist_ItemNotFound, $"Item {itemId} not found");

    public static Checklist Create(Guid accountId, Guid workspaceId, Guid itemId, string title, FractionalIndex position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(itemId);
        var normalizedTitle = NormalizeTitle(title);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);

        var checklist = new Checklist
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ItemId = itemId,
            Title = normalizedTitle,
            Position = position
        };

        checklist.SetAuditOnCreate(createdBy, createdAt);
        checklist.RaiseDomainEvent(new ChecklistCreatedDomainEvent(accountId, workspaceId, itemId, checklist.Id, checklist.Title, createdBy, createdAt));

        return checklist;
    }

    public ChecklistItem AddItem(string title, FractionalIndex position, Guid addedBy, DateTimeOffset addedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(addedBy);
        Guard.NotNull(position);
        var normalizedTitle = NormalizeTitle(title);
        var pending = PrepareAuditUpdate(addedBy, addedAt);
        var item = ChecklistItem.Create(Id, normalizedTitle, position);
        _items.Add(item);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemAddedDomainEvent(AccountId, WorkspaceId, Id, item.Id, normalizedTitle, ItemId, Title, addedBy, addedAt));
        return item;
    }

    /// <summary>Inverts the item's completion (the explicit /toggle command).</summary>
    public void ToggleItem(Guid itemId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var item = FindItem(itemId);
        ChangeCompletion(item, !item.IsDone, updatedBy, updatedAt);
    }

    /// <summary>
    /// Sets the item's completion to the desired state (WM-V3-APP-CHK-001); repeating
    /// the current state is a semantic no-op. Never implemented through toggle.
    /// </summary>
    public void SetItemCompletion(Guid itemId, bool done, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var item = FindItem(itemId);
        if (item.IsDone == done) return;
        ChangeCompletion(item, done, updatedBy, updatedAt);
    }

    private void ChangeCompletion(ChecklistItem item, bool done, Guid updatedBy, DateTimeOffset updatedAt)
    {
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        item.SetCompletion(done, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemToggledDomainEvent(AccountId, WorkspaceId, Id, item.Id, item.IsDone, ItemId, item.Title, Title, updatedBy, updatedAt));
    }

    public void RenameItem(Guid itemId, string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var normalizedTitle = NormalizeTitle(title);
        var item = FindItem(itemId);
        if (item.Title == normalizedTitle) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        item.Rename(normalizedTitle);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemRenamedDomainEvent(AccountId, WorkspaceId, Id, item.Id, normalizedTitle, updatedBy, updatedAt));
    }

    /// <summary>
    /// Moves one item to a position the caller resolved under the ChecklistItems
    /// ordering lock, rewriting only its key; the current key is a no-op.
    /// </summary>
    public void MoveItem(Guid itemId, FractionalIndex position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(position);
        var item = FindItem(itemId);
        if (item.Position.Value == position.Value) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        item.UpdatePosition(position);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistItemMovedDomainEvent(AccountId, WorkspaceId, Id, item.Id, position, updatedBy, updatedAt));
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
        RaiseDomainEvent(new ChecklistItemRemovedDomainEvent(AccountId, WorkspaceId, Id, item.Id, ItemId, item.Title, Title, updatedBy, updatedAt));
    }

    public void Rename(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var normalizedTitle = NormalizeTitle(title);
        if (Title == normalizedTitle) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Title = normalizedTitle;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistRenamedDomainEvent(AccountId, WorkspaceId, ItemId, Id, normalizedTitle, updatedBy, updatedAt));
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
        RaiseDomainEvent(new ChecklistMovedDomainEvent(AccountId, WorkspaceId, ItemId, Id, position, updatedBy, updatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new ChecklistDeletedDomainEvent(AccountId, WorkspaceId, ItemId, Id, Title, deletedBy, deletedAt));
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
