using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

namespace Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration;

/// <summary>
/// Runtime adapter of the reminder port: the Inbox notification goes through the notification pipeline every
/// other producer uses (preferences, muted threads, idempotency by key), in the caller's unit of work.
/// </summary>
public sealed class ItemReminderNotifierAdapter(ItemFollowerNotifier notifier) : IItemReminderNotifier
{
    public const string Title = "Asked for an update";

    public Task NotifyAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid senderUserId,
        Guid reminderId,
        DateTimeOffset remindedAt,
        IReadOnlyCollection<Guid> recipients,
        CancellationToken cancellationToken) =>
        notifier.NotifyAsync(
            accountId, workspaceId, itemId, senderUserId, sourceEventId: null,
            deduplicationScope: $"{reminderId:N}-{remindedAt.UtcTicks}",
            NotificationTypes.ItemReminderProducedType, Title, body: null,
            recipients, remindedAt, cancellationToken);
}
