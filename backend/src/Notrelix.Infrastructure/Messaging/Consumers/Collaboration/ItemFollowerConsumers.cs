using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using Notrelix.Domain.Collaboration.Preferences;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

[OrderingNotApplicable("Following is a set: adding a person who already follows is a no-op, so deliveries commute.")]
public sealed class BoardItemCreatedFollowConsumer : IConsumer<BoardItemCreatedIntegrationEventV2>
{
    private readonly ItemFollowerNotifier _notifier;

    public BoardItemCreatedFollowConsumer(ItemFollowerNotifier notifier) => _notifier = notifier;

    public async Task Consume(ConsumeContext<BoardItemCreatedIntegrationEventV2> context)
    {
        var msg = context.Message;
        if (msg.AccountId is not { } accountId || accountId == Guid.Empty
            || msg.WorkspaceId is not { } workspaceId || msg.ActorUserId is not { } creator)
            return;

        await _notifier.EnsureFollowingAsync(
            accountId, workspaceId, msg.ItemId, creator, creator, DateTimeOffset.UtcNow, context.CancellationToken);
    }
}

[OrderingNotApplicable("Following is a set and each notification is keyed by the assignment event and person; deliveries commute.")]
public sealed class BoardItemMemberAssignedFollowConsumer : IConsumer<BoardItemMemberAssignedIntegrationEvent>
{
    private readonly ItemFollowerNotifier _notifier;
    private readonly IWorkItemNotificationFacts _work;

    public BoardItemMemberAssignedFollowConsumer(ItemFollowerNotifier notifier, IWorkItemNotificationFacts work)
    {
        _notifier = notifier;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemMemberAssignedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        await _notifier.EnsureFollowingAsync(
            msg.AccountIdValue, msg.WorkspaceIdValue, msg.ItemId, msg.AssignedUserId, msg.AssignedBy, now, ct);

        var facts = await _work.FindAsync(msg.WorkspaceIdValue, msg.ItemId, null, ct);
        if (facts is null) return;

        // The assignee is told they were given the Item, unless they gave it to themselves.
        if (msg.AssignedUserId != msg.AssignedBy)
        {
            await _notifier.NotifyAsync(
                msg.AccountIdValue, msg.WorkspaceIdValue, msg.ItemId, msg.AssignedBy, msg.EventId,
                NotificationTypes.ItemAssignedProducedType, "You were assigned an item", null,
                [msg.AssignedUserId], now, ct);
        }

        // The other followers are told someone was added.
        await _notifier.NotifyFollowersAsync(
            msg.AccountIdValue, msg.WorkspaceIdValue, msg.ItemId, msg.AssignedBy, msg.EventId,
            NotificationTypes.ItemAssigneeAddedProducedType, "An item you follow has a new assignee", null,
            [msg.AssignedUserId], now, ct);
    }
}

[OrderingNotApplicable("Each status change notifies independently, keyed by the change event and person; notifications never supersede each other.")]
public sealed class BoardItemStatusChangedFollowConsumer : IConsumer<BoardItemFieldValueChangedIntegrationEvent>
{
    private readonly ItemFollowerNotifier _notifier;
    private readonly IWorkItemNotificationFacts _work;

    public BoardItemStatusChangedFollowConsumer(ItemFollowerNotifier notifier, IWorkItemNotificationFacts work)
    {
        _notifier = notifier;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemFieldValueChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId)
            return;

        var facts = await _work.FindAsync(workspaceId, msg.ItemId, msg.FieldId, context.CancellationToken);
        if (facts is not { ChangedFieldIsStatus: true }) return;

        await _notifier.NotifyFollowersAsync(
            accountId, workspaceId, msg.ItemId, msg.ActorUserId, msg.EventId,
            NotificationTypes.ItemStatusChangedProducedType,
            facts.StatusName is null ? "An item you follow changed status" : $"An item you follow is now {facts.StatusName}",
            facts.StatusName, [], DateTimeOffset.UtcNow, context.CancellationToken);
    }
}

[OrderingNotApplicable("Each comment notifies independently, keyed by the comment event and person; notifications never supersede each other.")]
public sealed class CommentCreatedFollowConsumer : IConsumer<CommentCreatedIntegrationEvent>
{
    private readonly ItemFollowerNotifier _notifier;
    private readonly IWorkItemNotificationFacts _work;

    public CommentCreatedFollowConsumer(ItemFollowerNotifier notifier, IWorkItemNotificationFacts work)
    {
        _notifier = notifier;
        _work = work;
    }

    public async Task Consume(ConsumeContext<CommentCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is { } commentWorkspaceId)
        {
            await _notifier.MarkMentionsRepliedAsync(
                commentWorkspaceId, msg.TargetType, msg.TargetId, msg.AuthorId, DateTimeOffset.UtcNow, context.CancellationToken);
        }

        if (msg.TargetType != ItemFollowerNotifier.BoardItemKind
            || msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId)
            return;

        var facts = await _work.FindAsync(workspaceId, msg.TargetId, null, context.CancellationToken);
        if (facts is null) return;

        await _notifier.NotifyFollowersAsync(
            accountId, workspaceId, msg.TargetId, msg.AuthorId, msg.EventId,
            NotificationTypes.ItemCommentedProducedType, $"New comment on {facts.Title}",
            ItemFollowerNotifier.Excerpt(msg.Body), [], DateTimeOffset.UtcNow, context.CancellationToken);
    }
}
