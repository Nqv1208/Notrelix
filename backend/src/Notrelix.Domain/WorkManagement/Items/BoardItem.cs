using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using System.Text.Json;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItem : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public Guid? ParentItemId { get; private set; }
    public string? ItemKey { get; private set; }
    public long? ItemSequence { get; private set; }
    public int ItemLevel { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }

    /// <summary>
    /// The Status option the Item held when it was completed, so that reopening can
    /// return it there. Null when none was recorded; cleared on reopen.
    /// </summary>
    public Guid? StatusBeforeCompletionOptionId { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Guid? ArchivedBy { get; private set; }

    /// <summary>
    /// True when this sub-item was archived because its parent was archived, so that
    /// restoring the parent restores exactly the sub-items it archived.
    /// </summary>
    public bool ArchivedWithParent { get; private set; }

    /// <summary>
    /// What the item is about, as Markdown the member writes. Work Management owns this text; it is
    /// not a field value and not a Document. Null means no description; the writer normalizes blank to null.
    /// </summary>
    public string? DescriptionMd { get; private set; }

    public const int DescriptionMaxLength = 20000;

    /// <summary>Cover colour token (<see cref="ItemCover.Colors"/>); null when the cover is an image or there is none.</summary>
    public string? CoverColor { get; private set; }

    /// <summary>The image attachment shown as the cover; null when the cover is a colour or there is none.</summary>
    public Guid? CoverAttachmentId { get; private set; }

    /// <summary>"strip" or "full"; set exactly when there is a cover.</summary>
    public string? CoverStyle { get; private set; }

    /// <summary>The repeat rule (<see cref="ItemRepeat.Rules"/>); null means the Item does not repeat.</summary>
    public string? RepeatRule { get; private set; }

    /// <summary>Minutes before the due time for the reminder; null is the default (one day), -1 is off.</summary>
    public int? ReminderOffsetMinutes { get; private set; }

    private readonly List<BoardItemValue> _fieldValues = new();
    public IReadOnlyCollection<BoardItemValue> FieldValues => _fieldValues.AsReadOnly();

    private BoardItem() : base() { }

    public static BoardItem CreateRoot(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? itemKey = null,
        long? itemSequence = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? dueAt = null,
        string? descriptionMd = null)
    {
        return CreateCore(
            accountId, workspaceId, boardId, groupId, name, position, createdBy, createdAt,
            parentItemId: null, itemLevel: 0,
            itemKey, itemSequence, startedAt, dueAt, descriptionMd);
    }

    public static BoardItem CreateChild(
        ItemParentPath parentPath,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? itemKey = null,
        long? itemSequence = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? dueAt = null)
    {
        Guard.NotNull(parentPath);
        return CreateCore(
            parentPath.AccountId, parentPath.WorkspaceId, parentPath.BoardId, groupId, name, position, createdBy, createdAt,
            parentPath.ParentItemId, parentPath.ChildLevel,
            itemKey, itemSequence, startedAt, dueAt);
    }

    /// <summary>
    /// Creates a sub-item of <paramref name="parent"/>. Sub-items are one level only
    /// and live in their parent's board and group.
    /// </summary>
    public static BoardItem CreateSubItem(
        BoardItem parent,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? itemKey = null,
        long? itemSequence = null)
    {
        Guard.NotNull(parent);
        parent.EnsureNotDeleted();
        parent.EnsureNotArchived();

        if (parent.ParentItemId is not null || parent.ItemLevel != 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_SubItemsAreOneLevel, "A sub-item cannot have sub-items.");

        var path = ItemParentPath.Create(parent.AccountId, parent.WorkspaceId, parent.BoardId, parent.Id, parent.ItemLevel, []);
        return CreateChild(path, parent.GroupId, name, position, createdBy, createdAt, itemKey, itemSequence);
    }

    private static BoardItem CreateCore(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid groupId,
        string name,
        FractionalIndex position,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? parentItemId,
        int itemLevel,
        string? itemKey,
        long? itemSequence,
        DateTimeOffset? startedAt,
        DateTimeOffset? dueAt,
        string? descriptionMd = null)
    {
        if (startedAt != null && dueAt != null && dueAt < startedAt)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_DueDateMustBeAfterStartDate, "Due date must be after start date.");
        var description = NormalizeDescription(descriptionMd);
        if (description is not null && description.Length > DescriptionMaxLength)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_DescriptionTooLong, $"Description cannot exceed {DescriptionMaxLength} characters.");
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(groupId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);
        Guard.NotNull(position);
        Guard.NotEmpty(accountId);
        Guard.NotNegative(itemLevel);

        var item = new BoardItem
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            GroupId = groupId,
            Name = name.Trim(),
            Position = position,
            ParentItemId = parentItemId,
            ItemKey = itemKey,
            ItemSequence = itemSequence,
            ItemLevel = itemLevel,
            StartedAt = startedAt,
            DueAt = dueAt,
            DescriptionMd = description
        };

        item.SetAuditOnCreate(createdBy, createdAt);
        item.RaiseDomainEvent(new BoardItemCreatedDomainEvent(accountId, workspaceId, boardId, groupId, item.Id, item.Name, createdBy, createdAt, parentItemId, itemLevel, item.Version));

        return item;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 500);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var oldName = Name;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemRenamedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldName, Name, updatedBy, updatedAt));
    }

    /// <summary>
    /// Records that the people in charge of the Item changed, so a write that was based on an older read of the
    /// Item is refused. The assignee rows are their own entities; this is the version the Item answers for them.
    /// </summary>
    public void NoteAssigneesChanged(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>
    /// Records that <paramref name="senderUserId"/>, who waits on this Item, asked the people in charge for an
    /// update. The Item's own state does not change; the fact is what Collaboration turns into Inbox entries.
    /// The 24 hour limit on asking again is a durable rule of <see cref="BoardItemReminder"/>, claimed by the
    /// caller before this fact is recorded.
    /// </summary>
    public void RecordOwnersReminded(Guid senderUserId, IReadOnlyCollection<Guid> recipientUserIds, DateTimeOffset remindedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(senderUserId);
        Guard.NotNull(recipientUserIds);

        var recipients = recipientUserIds.Where(id => id != senderUserId).Distinct().ToList();
        if (recipients.Count == 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Reminder_NobodyToRemind, "Nobody else is in charge of this item, so there is nobody to remind.");

        RaiseDomainEvent(new BoardItemOwnersRemindedDomainEvent(AccountId, WorkspaceId, Id, BoardId, senderUserId, recipients, remindedAt));
    }

    /// <summary>
    /// Replaces the item's description. Blank clears it. Writing the text it already holds changes nothing
    /// (no version, no audit). The description is not a business event: nothing outside the item consumes it.
    /// </summary>
    public void SetDescription(string? descriptionMd, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        var description = NormalizeDescription(descriptionMd);
        if (description is not null && description.Length > DescriptionMaxLength)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_DescriptionTooLong, $"Description cannot exceed {DescriptionMaxLength} characters.");
        if (DescriptionMd == description) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        DescriptionMd = description;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    private static string? NormalizeDescription(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r\n", "\n").TrimEnd();

    public void MoveToGroup(BoardGroupRef group, FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(group);
        Guard.NotNull(newPosition);

        if (group.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{WorkspaceId}', got '{group.WorkspaceId}'.");

        if (group.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardScopeMismatch, $"Board scope mismatch. Expected '{BoardId}', got '{group.BoardId}'.");

        if (GroupId == group.GroupId && Position == newPosition) return;

        var oldGroupId = GroupId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        GroupId = group.GroupId;
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemMovedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldGroupId, group.GroupId, newPosition.Value, updatedBy, updatedAt, Version));
    }

    /// <summary>
    /// Moves a root Item to a group of another board of the same workspace, taking that board's next
    /// item key. The values the Item held belong to the old board's fields and are dropped here; the
    /// caller writes the ones that carry over. The move is one version and one fact.
    /// </summary>
    public void MoveToBoard(BoardGroupRef targetGroup, FractionalIndex newPosition, string itemKey, long itemSequence, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(movedBy);
        Guard.NotNull(targetGroup);
        Guard.NotNull(newPosition);
        Guard.NotNullOrWhiteSpace(itemKey);

        if (ParentItemId is not null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_SubItemFollowsParentGroup, "A sub-item moves with its parent.");
        if (targetGroup.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{WorkspaceId}', got '{targetGroup.WorkspaceId}'.");
        if (targetGroup.BoardId == BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardScopeMismatch, "The item is already on this board.");

        var fromBoardId = BoardId;
        var fromGroupId = GroupId;
        var pending = PrepareAuditUpdate(movedBy, movedAt);
        ApplyBoardChange(targetGroup, newPosition, itemKey, itemSequence);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemMovedToBoardDomainEvent(AccountId, WorkspaceId, Id, fromBoardId, BoardId, fromGroupId, GroupId, itemKey, movedBy, movedAt, Version));
    }

    /// <summary>A sub-item follows its parent to the parent's new board and group, with its own new key.</summary>
    public void FollowParentToBoard(BoardGroupRef targetGroup, string itemKey, long itemSequence, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(movedBy);
        Guard.NotNull(targetGroup);
        if (ParentItemId is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_NotASubItem, "Only a sub-item follows its parent.");

        var pending = PrepareAuditUpdate(movedBy, movedAt);
        ApplyBoardChange(targetGroup, Position, itemKey, itemSequence);
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    private void ApplyBoardChange(BoardGroupRef targetGroup, FractionalIndex position, string itemKey, long itemSequence)
    {
        BoardId = targetGroup.BoardId;
        GroupId = targetGroup.GroupId;
        Position = position;
        ItemKey = itemKey;
        ItemSequence = itemSequence;
        _fieldValues.Clear();
    }

    /// <summary>
    /// Moves the Item to another group because its own group is being deleted. Unlike a
    /// user move this also applies to archived Items, so no Item is left in the deleted
    /// group. A null <paramref name="newPosition"/> keeps the position (child Items are
    /// not ordered within the group).
    /// </summary>
    public void MoveToGroupOnGroupRemoval(BoardGroupRef group, FractionalIndex? newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(group);

        if (group.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{WorkspaceId}', got '{group.WorkspaceId}'.");

        if (group.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_BoardScopeMismatch, $"Board scope mismatch. Expected '{BoardId}', got '{group.BoardId}'.");

        var oldGroupId = GroupId;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        GroupId = group.GroupId;
        if (newPosition is not null) Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemMovedDomainEvent(AccountId, WorkspaceId, Id, BoardId, oldGroupId, group.GroupId, Position.Value, updatedBy, updatedAt, Version));
    }

    public void UpdateFieldValue(BoardField field, FieldValue newValue, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(field);
        Guard.NotNull(newValue);

        if (field.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_BelongsToDifferentWorkspace, "Field belongs to a different workspace.");

        if (field.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_DoesNotBelongToBoard, "Field does not belong to this board.");

        if (field.IsDeleted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotUpdateDeleted, "Cannot update value for a deleted field.");

        // A system field's definition is protected by the field itself (it cannot be
        // deleted). The Item's Status is ordinary Item data written here; the other
        // system fields mirror state the Item owns directly (its name, DueAt, its
        // assignee members), so a raw value would be a parallel truth and is rejected.
        if (field.IsSystem && field.Type != FieldType.Status)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Field_CannotUpdateSystem,
                $"The system field '{field.Name}' is written through the Item's own path, not as a field value.");

        if (field.Type == FieldType.Vote)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotWriteVote, "A vote is cast by the person voting, not written as a value.");

        if (field.Type is FieldType.Formula or FieldType.Rollup)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_CannotWriteComputed, "Cannot manually write to a computed field (Formula or Rollup).");

        FieldValueValidator.Validate(newValue, field.Type, field.Settings);

        // Clearing (canonical JSON null) needs no option; FieldValueValidator accepts it.
        if (!newValue.IsEmpty && field.Type is FieldType.Select or FieldType.Status)
        {
            var optionId = ParseOptionId(newValue, WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue);
            if (!field.Options.Any(option => option.Id == optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Field_InvalidOptionValue, $"Value '{optionId}' is not a valid option for field '{field.Name}'.");
        }
        else if (!newValue.IsEmpty && field.Type == FieldType.MultiSelect)
        {
            var selectedIds = ParseDistinctOptionIds(newValue);
            var allowedIds = field.Options.Select(option => option.Id).ToHashSet();
            if (!selectedIds.All(allowedIds.Contains))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, $"Value contains an option that is not valid for field '{field.Name}'.");
        }

        var existingValue = _fieldValues.FirstOrDefault(fv => fv.FieldId == field.Id);
        var oldValue = existingValue?.Value ?? FieldValue.Empty();

        if (existingValue != null && existingValue.Value == newValue)
            return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);

        if (existingValue == null)
        {
            _fieldValues.Add(BoardItemValue.Create(Id, field.Id, newValue));
        }
        else
        {
            existingValue.UpdateValue(newValue);
        }

        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemFieldValueChangedDomainEvent(AccountId, WorkspaceId, Id, BoardId, field.Id, oldValue, newValue, updatedBy, updatedAt));
    }

    private static Guid ParseOptionId(FieldValue value, string ruleCode)
    {
        var raw = value.Data.Value.Trim('"');
        if (!Guid.TryParse(raw, out var optionId))
            throw new BusinessRuleException(ruleCode, $"Value '{raw}' is not a valid option id.");
        return optionId;
    }

    private static IReadOnlyList<Guid> ParseDistinctOptionIds(FieldValue value)
    {
        using var msDoc = JsonDocument.Parse(value.Data.Value);
        if (msDoc.RootElement.ValueKind != JsonValueKind.Array)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, "Value for field type MultiSelect must be an array of option IDs.");

        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var element in msDoc.RootElement.EnumerateArray())
        {
            var raw = element.GetString();
            if (!Guid.TryParse(raw, out var optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_InvalidMultiSelectValue, $"Value '{raw}' is not a valid option id.");

            if (!seen.Add(optionId))
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldValue_DuplicateOptionId, $"Option id '{optionId}' appears more than once.");

            ids.Add(optionId);
        }

        return ids;
    }

    public void MoveToRoot(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        if (ParentItemId is null && ItemLevel == 0) return;

        var previousParentItemId = ParentItemId;
        var previousLevel = ItemLevel;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentItemId = null;
        ItemLevel = 0;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemParentChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, previousParentItemId, null, previousLevel, 0, updatedBy, updatedAt));
    }

    /// <summary>Places a sub-item among its parent's sub-items.</summary>
    public void RepositionWithinParent(FractionalIndex newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(newPosition);

        if (ParentItemId is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_NotASubItem, "Only a sub-item is ordered under a parent.");

        if (Position == newPosition) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Position = newPosition;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemMovedDomainEvent(AccountId, WorkspaceId, Id, BoardId, GroupId, GroupId, newPosition.Value, updatedBy, updatedAt, Version));
    }

    public void MoveUnder(ItemParentPath parentPath, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(parentPath);

        if (parentPath.AccountId != AccountId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameAccount, $"Parent item must belong to the same account. Expected '{AccountId}', got '{parentPath.AccountId}'.");

        if (parentPath.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameWorkspace, $"Parent item must belong to the same workspace. Expected '{WorkspaceId}', got '{parentPath.WorkspaceId}'.");

        if (parentPath.BoardId != BoardId)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentMustBelongToSameBoard, $"Parent item must belong to the same board. Expected '{BoardId}', got '{parentPath.BoardId}'.");

        if (parentPath.ParentItemId == Id)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CannotBeOwnParent, "An item cannot be its own parent.");

        if (parentPath.AncestorIds.Contains(Id))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_ParentAssignmentWouldCreateCycle, "Item parent assignment would create a cycle.");

        var newLevel = parentPath.ChildLevel;
        if (ParentItemId == parentPath.ParentItemId && ItemLevel == newLevel) return;

        var previousParentItemId = ParentItemId;
        var previousLevel = ItemLevel;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ParentItemId = parentPath.ParentItemId;
        ItemLevel = newLevel;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemParentChangedDomainEvent(AccountId, WorkspaceId, BoardId, Id, previousParentItemId, ParentItemId, previousLevel, ItemLevel, updatedBy, updatedAt));
    }

    /// <summary>
    /// Sets the cover to a colour or an image attachment (exactly one), or clears it when both are null. The
    /// same cover again changes nothing. That the attachment is an image of this Item is checked by Application.
    /// </summary>
    public void SetCover(string? color, Guid? attachmentId, string? style, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        var clearing = color is null && attachmentId is null;
        if (clearing) style = null;
        else if ((color is not null && attachmentId is not null)
                 || (color is not null && !ItemCover.IsColor(color))
                 || (attachmentId == Guid.Empty)
                 || !ItemCover.IsStyle(style))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_InvalidCover, "A cover is one colour or one image, shown as a strip or over the whole card.");

        if (CoverColor == color && CoverAttachmentId == attachmentId && CoverStyle == style) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        CoverColor = color;
        CoverAttachmentId = attachmentId;
        CoverStyle = style;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>Sets or (null) clears the repeat rule. A repeating Item needs a due date to advance.</summary>
    public void SetRepeat(string? rule, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        if (rule is not null && !ItemRepeat.IsRule(rule))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_InvalidRepeat, "Unknown repeat rule.");
        if (rule is not null && DueAt is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_RepeatNeedsDueDate, "Set a due date before the item repeats.");
        if (RepeatRule == rule) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        RepeatRule = rule;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>Sets the reminder offset in minutes before the due time; the default (one day) is stored as null.</summary>
    public void SetReminder(int offsetMinutes, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        if (!ItemReminder.IsOffset(offsetMinutes))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_InvalidReminder, "Unknown reminder offset.");

        int? next = offsetMinutes == ItemReminder.DefaultMinutes ? null : offsetMinutes;
        if (ReminderOffsetMinutes == next) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ReminderOffsetMinutes = next;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>True when completing the Item moves it to its next occurrence instead of closing it.</summary>
    public bool RepeatsOnCompletion => RepeatRule is not null && DueAt is not null && !CompletedAt.HasValue;

    /// <summary>
    /// Completes the Item. A repeating Item with a due date is not left completed: it stays open and its due
    /// (and start) date moves to the next occurrence after the completion, in this one change. Completing an
    /// already completed Item is a no-op; a retry under the same idempotency key replays the first result.
    /// </summary>
    public void CompleteOccurrence(DateTimeOffset completedAt, Guid updatedBy, DateTimeOffset updatedAt, Guid? statusBeforeCompletionOptionId = null)
    {
        if (RepeatRule is null || DueAt is null)
        {
            Complete(completedAt, updatedBy, updatedAt, statusBeforeCompletionOptionId);
            return;
        }

        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        if (completedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CompletedAtRequired, "Completion timestamp must be provided.");

        if (CompletedAt.HasValue) return;

        var previousDue = DueAt.Value;
        var nextDue = ItemRepeat.NextDue(previousDue, RepeatRule, completedAt);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        if (StartedAt is { } start) StartedAt = start + (nextDue - previousDue);
        DueAt = nextDue;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemRepeatedDomainEvent(AccountId, WorkspaceId, BoardId, Id, completedAt, previousDue, nextDue, updatedBy, updatedAt));
    }

    public void SetTimeline(DateTimeOffset? startedAt, DateTimeOffset? dueAt, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);
        if (startedAt != null && dueAt != null && dueAt < startedAt)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_DueDateMustBeAfterStartDate, "Due date must be after start date.");

        if (StartedAt == startedAt && DueAt == dueAt) return;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        StartedAt = startedAt;
        DueAt = dueAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemTimelineSetDomainEvent(AccountId, WorkspaceId, BoardId, Id, startedAt, dueAt, updatedBy, updatedAt));
    }

    public void Complete(DateTimeOffset completedAt, Guid updatedBy, DateTimeOffset updatedAt, Guid? statusBeforeCompletionOptionId = null)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        if (completedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CompletedAtRequired, "Completion timestamp must be provided.");

        if (CompletedAt.HasValue) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        CompletedAt = completedAt;
        CompletedBy = updatedBy;
        StatusBeforeCompletionOptionId = statusBeforeCompletionOptionId;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemCompletedDomainEvent(AccountId, WorkspaceId, BoardId, Id, completedAt, updatedBy, updatedAt));
    }

    /// <summary>
    /// Keeps completion in step with the Item's Status: a status in the done
    /// <paramref name="category"/> completes the Item at <paramref name="changedAt"/>
    /// (an already completed Item keeps its completion time); any other category,
    /// or no status, reopens it. The category is supplied by the caller from the
    /// Board's Status field.
    /// </summary>
    public void SyncCompletionWithStatus(WorkStatusCategory? category, Guid changedBy, DateTimeOffset changedAt, Guid? statusBeforeCompletionOptionId = null)
    {
        if (category == WorkStatusCategory.Done)
            Complete(changedAt, changedBy, changedAt, statusBeforeCompletionOptionId);
        else
            Reopen(changedBy, changedAt);
    }

    public void Reopen(Guid reopenedBy, DateTimeOffset reopenedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(reopenedBy);

        if (!CompletedAt.HasValue) return;

        var pending = PrepareAuditUpdate(reopenedBy, reopenedAt);
        CompletedAt = null;
        CompletedBy = null;
        StatusBeforeCompletionOptionId = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemReopenedDomainEvent(AccountId, WorkspaceId, BoardId, Id, reopenedBy, reopenedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (IsArchived) return;
        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        IsArchived = true;
        ArchivedAt = archivedAt;
        ArchivedBy = archivedBy;
        ArchivedWithParent = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemArchivedDomainEvent(AccountId, WorkspaceId, Id, BoardId, archivedBy, archivedAt, Version));
    }

    public void Unarchive(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unarchivedBy);
        if (!IsArchived) return;
        var pending = PrepareAuditUpdate(unarchivedBy, unarchivedAt);
        IsArchived = false;
        ArchivedAt = null;
        ArchivedBy = null;
        ArchivedWithParent = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new BoardItemUnarchivedDomainEvent(AccountId, WorkspaceId, BoardId, Id, unarchivedBy, unarchivedAt));
    }

    /// <summary>
    /// Archives a sub-item because its parent was archived. A sub-item that is already
    /// archived on its own stays as it is, so restoring the parent does not restore it.
    /// </summary>
    public void ArchiveWithParent(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (ParentItemId is null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_NotASubItem, "Only a sub-item is archived with its parent.");
        if (IsDeleted || IsArchived) return;
        Archive(archivedBy, archivedAt);
        ArchivedWithParent = true;
    }

    /// <summary>Restores a sub-item that was archived with its parent; any other sub-item is left alone.</summary>
    public void UnarchiveWithParent(Guid unarchivedBy, DateTimeOffset unarchivedAt)
    {
        if (!ArchivedWithParent) return;
        Unarchive(unarchivedBy, unarchivedAt);
    }

    /// <param name="replacementPosition">
    /// Supplied by the caller (under the GroupItems ordering lock) when the item's
    /// previous key is now held by an active sibling; applied in the same version.
    /// </param>
    public void Restore(Guid restoredBy, DateTimeOffset restoredAt, FractionalIndex? replacementPosition = null)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        if (replacementPosition is not null) Position = replacementPosition;
        IncrementVersion();
        RaiseDomainEvent(new BoardItemRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Item_CannotModifyArchived,
                "Cannot modify an archived board item.");
    }
}
