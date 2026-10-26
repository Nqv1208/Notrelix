using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.Collaboration.Preferences;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

[OrderingNotApplicable("Each approval request notifies its approver once, keyed by the request event and person; deliveries commute.")]
public sealed class ApprovalRequestedNotificationConsumer : IConsumer<ApprovalRequestedIntegrationEvent>
{
    private readonly ItemFollowerNotifier _notifier;

    public ApprovalRequestedNotificationConsumer(ItemFollowerNotifier notifier) => _notifier = notifier;

    public async Task Consume(ConsumeContext<ApprovalRequestedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.TargetKind != ItemFollowerNotifier.BoardItemKind || msg.ApproverUserId == msg.RequestedBy) return;

        // The approver is told they were asked; a note, when given, is the excerpt.
        await _notifier.NotifyAsync(
            msg.AccountIdValue, msg.WorkspaceIdValue, msg.TargetId, msg.RequestedBy, msg.EventId,
            NotificationTypes.ApprovalRequestedProducedType, "Approval requested", ItemFollowerNotifier.Excerpt(msg.Note),
            [msg.ApproverUserId], DateTimeOffset.UtcNow, context.CancellationToken);
    }
}

[OrderingNotApplicable("Each decision notifies its requester once, keyed by the decision event and person; deliveries commute.")]
public sealed class ApprovalDecidedNotificationConsumer : IConsumer<ApprovalDecidedIntegrationEvent>
{
    private readonly ItemFollowerNotifier _notifier;

    public ApprovalDecidedNotificationConsumer(ItemFollowerNotifier notifier) => _notifier = notifier;

    public async Task Consume(ConsumeContext<ApprovalDecidedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.TargetKind != ItemFollowerNotifier.BoardItemKind || msg.DecidedBy == msg.RequestedBy) return;

        await _notifier.NotifyAsync(
            msg.AccountIdValue, msg.WorkspaceIdValue, msg.TargetId, msg.DecidedBy, msg.EventId,
            msg.Approved ? NotificationTypes.ApprovalApprovedProducedType : NotificationTypes.ApprovalRejectedProducedType,
            msg.Approved ? "Your approval request was approved" : "Your approval request was rejected",
            ItemFollowerNotifier.Excerpt(msg.Note),
            [msg.RequestedBy], DateTimeOffset.UtcNow, context.CancellationToken);
    }
}
