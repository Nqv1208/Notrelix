using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.WorkManagement.Public.ItemActivity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

// Item history: each consumer turns one Work Management fact into one append-only history row keyed by the
// source event id (ItemActivityRecorder ignores a redelivery), so deliveries commute and readers sort by
// the fact's own time.
internal static class ItemActivityOrdering
{
    public const string NotApplicable =
        "Append-only Item history row keyed by the source event id with its own OccurredAt; rows do not supersede one another and readers sort by occurrence time.";
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemCreatedActivityConsumer : IConsumer<BoardItemCreatedIntegrationEventV2>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemCreatedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemCreatedIntegrationEventV2> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is not { } workspaceId) return Task.CompletedTask;
        return _recorder.RecordAsync(
            workspaceId, msg.ItemId, msg.BoardId, ItemActivityTypes.Created, msg.EventId, msg.ActorUserId, msg.OccurredAt,
            "created this item", msg.Title, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemRenamedActivityConsumer : IConsumer<BoardItemRenamedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemRenamedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemRenamedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is not { } workspaceId) return Task.CompletedTask;
        var from = ItemChangeWords.Truncate(msg.OldName);
        var to = ItemChangeWords.Truncate(msg.NewName);
        return _recorder.RecordAsync(
            workspaceId, msg.ItemId, msg.BoardId, ItemActivityTypes.Renamed, msg.EventId, msg.ActorUserId, msg.OccurredAt,
            $"renamed this item from \"{from}\" to \"{to}\"", to,
            new Dictionary<string, string?> { ["from"] = from, ["to"] = to }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemFieldChangedActivityConsumer : IConsumer<BoardItemFieldValueChangedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;
    private readonly IWorkItemActivityFacts _work;

    public ItemFieldChangedActivityConsumer(ItemActivityRecorder recorder, IWorkItemActivityFacts work)
    {
        _recorder = recorder;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemFieldValueChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        if (msg.WorkspaceId is not { } workspaceId) return;

        var field = await _work.FindFieldAsync(workspaceId, msg.FieldId, ct);
        var (from, to) = ItemChangeWords.Describe(field, msg.OldValue, msg.NewValue);
        var isStatus = field is { IsStatus: true };
        var subject = isStatus ? "the status" : field is null ? "a field" : field.Name;
        var title = from is not null && to is not null
            ? $"changed {subject} from {from} to {to}"
            : to is not null ? $"set {subject} to {to}" : $"changed {subject}";

        await _recorder.RecordAsync(
            workspaceId, msg.ItemId, msg.BoardId,
            isStatus ? ItemActivityTypes.StatusChanged : ItemActivityTypes.FieldChanged,
            msg.EventId, msg.ActorUserId, msg.OccurredAt, title, null,
            new Dictionary<string, string?> { ["fieldName"] = field?.Name, ["from"] = from, ["to"] = to }, ct,
            automationExecutionId: msg.CausationId);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemMovedActivityConsumer : IConsumer<BoardItemMovedIntegrationEventV2>
{
    private readonly ItemActivityRecorder _recorder;
    private readonly IWorkItemActivityFacts _work;

    public ItemMovedActivityConsumer(ItemActivityRecorder recorder, IWorkItemActivityFacts work)
    {
        _recorder = recorder;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemMovedIntegrationEventV2> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        // Only a change of group is history; reordering inside a group is not.
        if (msg.WorkspaceId is not { } workspaceId
            || msg.OldGroupId is not { } oldGroupId || msg.NewGroupId is not { } newGroupId
            || oldGroupId == newGroupId)
            return;

        var from = ItemChangeWords.Truncate(await _work.FindGroupNameAsync(workspaceId, oldGroupId, ct));
        var to = ItemChangeWords.Truncate(await _work.FindGroupNameAsync(workspaceId, newGroupId, ct));
        await _recorder.RecordAsync(
            workspaceId, msg.ItemId, msg.BoardId, ItemActivityTypes.MovedToGroup, msg.EventId, msg.ActorUserId, msg.OccurredAt,
            from is not null && to is not null ? $"moved this item from {from} to {to}" : "moved this item to another group", null,
            new Dictionary<string, string?> { ["from"] = from, ["to"] = to }, ct,
            automationExecutionId: msg.AutomationExecutionId);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemMovedToBoardActivityConsumer : IConsumer<BoardItemMovedToBoardIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;
    private readonly IWorkItemActivityFacts _work;

    public ItemMovedToBoardActivityConsumer(ItemActivityRecorder recorder, IWorkItemActivityFacts work)
    {
        _recorder = recorder;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemMovedToBoardIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var from = ItemChangeWords.Truncate(await _work.FindBoardNameAsync(msg.WorkspaceIdValue, msg.FromBoardId, ct));
        var to = ItemChangeWords.Truncate(await _work.FindBoardNameAsync(msg.WorkspaceIdValue, msg.ToBoardId, ct));
        await _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, msg.ToBoardId, ItemActivityTypes.MovedToBoard, msg.EventId, msg.MovedBy,
            msg.OccurredAt,
            from is not null && to is not null ? $"moved this item from {from} to {to}" : "moved this item to another board", null,
            new Dictionary<string, string?> { ["from"] = from, ["to"] = to, ["itemKey"] = msg.ItemKey }, ct);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemAssignedActivityConsumer : IConsumer<BoardItemMemberAssignedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemAssignedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemMemberAssignedIntegrationEvent> context)
    {
        var msg = context.Message;
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.Assigned, msg.EventId, msg.AssignedBy, msg.OccurredAt,
            msg.AssignedUserId == msg.AssignedBy ? "assigned themselves to this item" : "assigned someone to this item", null,
            new Dictionary<string, string?> { ["personId"] = msg.AssignedUserId.ToString() }, context.CancellationToken,
            automationExecutionId: msg.CausationId);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemUnassignedActivityConsumer : IConsumer<BoardItemMemberUnassignedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemUnassignedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemMemberUnassignedIntegrationEvent> context)
    {
        var msg = context.Message;
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.Unassigned, msg.EventId, msg.UnassignedBy, msg.OccurredAt,
            msg.UnassignedUserId == msg.UnassignedBy ? "unassigned themselves from this item" : "removed someone from this item", null,
            new Dictionary<string, string?> { ["personId"] = msg.UnassignedUserId.ToString() }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemLabelAddedActivityConsumer : IConsumer<BoardItemLabelAddedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemLabelAddedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemLabelAddedIntegrationEvent> context)
    {
        var msg = context.Message;
        var label = ItemChangeWords.Truncate(msg.LabelName);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.LabelAdded, msg.EventId, msg.AddedBy, msg.OccurredAt,
            $"added the label \"{label}\"", null,
            new Dictionary<string, string?> { ["to"] = label }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemLabelRemovedActivityConsumer : IConsumer<BoardItemLabelRemovedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemLabelRemovedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemLabelRemovedIntegrationEvent> context)
    {
        var msg = context.Message;
        var label = ItemChangeWords.Truncate(msg.LabelName);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.LabelRemoved, msg.EventId, msg.RemovedBy, msg.OccurredAt,
            $"removed the label \"{label}\"", null,
            new Dictionary<string, string?> { ["to"] = label }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemDatesChangedActivityConsumer : IConsumer<BoardItemDatesChangedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemDatesChangedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    private static string Words(DateTimeOffset? start, DateTimeOffset? due) =>
        start is null && due is null
            ? "no dates"
            : $"{start?.UtcDateTime.ToString("yyyy-MM-dd") ?? "…"} → {due?.UtcDateTime.ToString("yyyy-MM-dd") ?? "…"}";

    public Task Consume(ConsumeContext<BoardItemDatesChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var from = Words(msg.PreviousStartedAt, msg.PreviousDueAt);
        var to = Words(msg.StartedAt, msg.DueAt);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, msg.BoardId, ItemActivityTypes.DatesChanged, msg.EventId, msg.ChangedBy, msg.OccurredAt,
            "changed the dates", null,
            new Dictionary<string, string?> { ["from"] = from, ["to"] = to }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemArchivedActivityConsumer : IConsumer<BoardItemArchivedIntegrationEventV2>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemArchivedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemArchivedIntegrationEventV2> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is not { } workspaceId) return Task.CompletedTask;
        return _recorder.RecordAsync(
            workspaceId, msg.ItemId, msg.BoardId, ItemActivityTypes.Archived, msg.EventId, msg.ActorUserId, msg.OccurredAt,
            "archived this item", null, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemUnarchivedActivityConsumer : IConsumer<BoardItemUnarchivedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemUnarchivedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemUnarchivedIntegrationEvent> context)
    {
        var msg = context.Message;
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, msg.BoardId, ItemActivityTypes.Unarchived, msg.EventId, msg.UnarchivedBy,
            msg.OccurredAt, "restored this item from the archive", null, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemDeletedActivityConsumer : IConsumer<BoardItemDeletedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemDeletedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemDeletedIntegrationEvent> context)
    {
        var msg = context.Message;
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, msg.BoardId, ItemActivityTypes.Deleted, msg.EventId, msg.DeletedBy,
            msg.OccurredAt, "deleted this item", null, null, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemRestoredActivityConsumer : IConsumer<BoardItemRestoredIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemRestoredActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemRestoredIntegrationEvent> context)
    {
        var msg = context.Message;
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, msg.BoardId, ItemActivityTypes.Restored, msg.EventId, msg.RestoredBy,
            msg.OccurredAt, "restored this item from deleted", null, null, context.CancellationToken);
    }
}

// Checklist history reuses the read model's keys: `to` is the checklist (or to-do) title and, for a to-do, `fieldName` is its checklist.
[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistAddedActivityConsumer : IConsumer<BoardItemChecklistAddedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistAddedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistAddedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.ChecklistAdded, msg.EventId, msg.AddedBy, msg.OccurredAt,
            $"added the checklist \"{list}\"", null,
            new Dictionary<string, string?> { ["to"] = list }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistRenamedActivityConsumer : IConsumer<BoardItemChecklistRenamedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistRenamedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistRenamedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.ChecklistRenamed, msg.EventId, msg.RenamedBy, msg.OccurredAt,
            $"renamed a checklist to \"{list}\"", null,
            new Dictionary<string, string?> { ["to"] = list }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistDeletedActivityConsumer : IConsumer<BoardItemChecklistDeletedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistDeletedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistDeletedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.ChecklistDeleted, msg.EventId, msg.DeletedBy, msg.OccurredAt,
            $"deleted the checklist \"{list}\"", null,
            new Dictionary<string, string?> { ["to"] = list }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistItemAddedActivityConsumer : IConsumer<BoardItemChecklistItemAddedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistItemAddedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistItemAddedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        var todo = ItemChangeWords.Truncate(msg.ChecklistItemTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.ChecklistItemAdded, msg.EventId, msg.AddedBy, msg.OccurredAt,
            $"added \"{todo}\" to the checklist \"{list}\"", null,
            new Dictionary<string, string?> { ["fieldName"] = list, ["to"] = todo }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistItemCompletedActivityConsumer : IConsumer<BoardItemChecklistItemCompletionChangedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistItemCompletedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistItemCompletionChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        var todo = ItemChangeWords.Truncate(msg.ChecklistItemTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, msg.IsCompleted ? ItemActivityTypes.ChecklistItemCompleted : ItemActivityTypes.ChecklistItemReopened, msg.EventId, msg.ChangedBy, msg.OccurredAt,
            msg.IsCompleted ? $"completed \"{todo}\" in the checklist \"{list}\"" : $"marked \"{todo}\" as not done in the checklist \"{list}\"", null,
            new Dictionary<string, string?> { ["fieldName"] = list, ["to"] = todo }, context.CancellationToken);
    }
}

[OrderingNotApplicable(ItemActivityOrdering.NotApplicable)]
public sealed class ItemChecklistItemRemovedActivityConsumer : IConsumer<BoardItemChecklistItemRemovedIntegrationEvent>
{
    private readonly ItemActivityRecorder _recorder;

    public ItemChecklistItemRemovedActivityConsumer(ItemActivityRecorder recorder) => _recorder = recorder;

    public Task Consume(ConsumeContext<BoardItemChecklistItemRemovedIntegrationEvent> context)
    {
        var msg = context.Message;
        var list = ItemChangeWords.Truncate(msg.ChecklistTitle);
        var todo = ItemChangeWords.Truncate(msg.ChecklistItemTitle);
        return _recorder.RecordAsync(
            msg.WorkspaceIdValue, msg.ItemId, null, ItemActivityTypes.ChecklistItemRemoved, msg.EventId, msg.RemovedBy, msg.OccurredAt,
            $"removed \"{todo}\" from the checklist \"{list}\"", null,
            new Dictionary<string, string?> { ["fieldName"] = list, ["to"] = todo }, context.CancellationToken);
    }
}
