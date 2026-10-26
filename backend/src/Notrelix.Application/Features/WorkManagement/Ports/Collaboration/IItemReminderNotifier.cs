namespace Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

/// <summary>
/// Cross-context write port: Work Management asks Collaboration to tell people in their Inbox that someone
/// waits on their work. Collaboration owns the notification, its preference check and its idempotency.
/// </summary>
public interface IItemReminderNotifier
{
    /// <summary>
    /// One in-app reminder per recipient, naming <paramref name="senderUserId"/>. Idempotent by
    /// <paramref name="reminderId"/> and <paramref name="remindedAt"/>, so a retried request adds nothing.
    /// </summary>
    Task NotifyAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid senderUserId,
        Guid reminderId,
        DateTimeOffset remindedAt,
        IReadOnlyCollection<Guid> recipients,
        CancellationToken cancellationToken);
}
