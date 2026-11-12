using Notrelix.Application.Features.Collaboration.Notifications;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;

/// <summary>
/// Collaboration-owned implementation of the Chat notification intent boundary (UC-CHT-G09). It is the one notification path: the same
/// notification rows, preference sets, thread mutes and deduplication index the Collaboration consumers use. One notification per
/// (message, recipient, reason) is guaranteed by the unique deduplication key, so a redelivered fact adds nothing. The actor is never
/// a recipient. A chat notification is stored against the space (<see cref="NotificationTypes.ChatSpaceResourceKind"/>), so a person's
/// thread mute of that space silences it and the Inbox shows it only while the person can still see the space.
/// </summary>
public sealed class PostgresChatNotificationIntents : IChatNotificationIntents
{
    private const string DeduplicationIndexName = "ux_notifications_items_dedup";

    private readonly INotificationDbContext _notifications;
    private readonly ILogger<PostgresChatNotificationIntents> _logger;

    public PostgresChatNotificationIntents(INotificationDbContext notifications, ILogger<PostgresChatNotificationIntents> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ChatNotificationDelivery> DeliverAsync(
        ChatNotificationSource source,
        IReadOnlyCollection<ChatNotificationIntent> intents,
        CancellationToken cancellationToken)
    {
        // The actor never hears about their own message, and an unknown reason has no notification type: both are dropped here, not trusted to the caller.
        var wanted = intents
            .Where(i => i.RecipientUserId != source.ActorUserId && i.RecipientUserId != Guid.Empty && ChatNotificationReasons.IsKnown(i.Reason))
            .GroupBy(i => (i.RecipientUserId, i.Reason))
            .Select(g => g.Key)
            .ToList();
        if (wanted.Count == 0)
            return ChatNotificationDelivery.None;

        var recipients = wanted.Select(w => w.RecipientUserId).Distinct().ToList();
        var keys = wanted.Select(w => Key(source, w.RecipientUserId, w.Reason)).ToList();

        var existing = (await _notifications.NotificationItems
                .AsNoTracking()
                .Where(n => n.DeduplicationKey != null && keys.Contains(n.DeduplicationKey))
                .Select(n => n.DeduplicationKey!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var muted = (await _notifications.NotificationThreadMutes
                .AsNoTracking()
                .Where(m => m.ResourceKind == NotificationTypes.ChatSpaceResourceKind
                            && m.ResourceId == source.SpaceId
                            && recipients.Contains(m.UserId))
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var sets = await _notifications.NotificationPreferenceSets
            .AsNoTracking()
            .Include(s => s.Entries)
            .Where(s => s.AccountId == source.AccountId && s.WorkspaceId == source.WorkspaceId && recipients.Contains(s.UserId))
            .ToListAsync(cancellationToken);

        var preview = NotificationExcerpt.From(source.Preview);
        var duplicates = 0;
        var suppressed = 0;
        var delivered = new List<(string Key, Guid UserId)>();

        foreach (var (userId, reason) in wanted)
        {
            var key = Key(source, userId, reason);
            if (existing.Contains(key))
            {
                duplicates++;
                continue;
            }

            var producedType = ProducedType(reason);
            var preferenceType = NotificationTypes.FromProducedType(producedType)!;
            var set = sets.FirstOrDefault(s => s.UserId == userId);
            var enabled = set?.IsEnabled(preferenceType, NotificationChannels.InApp)
                ?? NotificationPreferenceDefaults.IsEnabled(preferenceType, NotificationChannels.InApp);
            if (!enabled || muted.Contains(userId))
            {
                suppressed++;
                continue;
            }

            var item = NotificationItemRecord.Create(
                accountId: source.AccountId,
                workspaceId: source.WorkspaceId,
                sourceContext: "chat",
                notificationType: producedType,
                severity: NotificationSeverity.Info,
                title: Title(reason),
                createdAt: source.At,
                actorUserId: source.ActorUserId,
                sourceEventId: source.SourceEventId,
                subjectType: "ChatMessage",
                subjectId: source.MessageId,
                resourceType: NotificationTypes.ChatSpaceResourceKind,
                resourceId: source.SpaceId,
                body: preview,
                deduplicationKey: key);
            _notifications.NotificationItems.Add(item);
            _notifications.NotificationRecipients.Add(NotificationRecipientRecord.Create(
                accountId: source.AccountId,
                notificationId: item.Id,
                workspaceId: source.WorkspaceId,
                recipientUserId: userId,
                createdAt: source.At));
            delivered.Add((key, userId));
        }

        if (delivered.Count == 0)
            return new ChatNotificationDelivery(0, duplicates, suppressed);

        try
        {
            await _notifications.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDeduplicationViolation(ex))
        {
            // A concurrent delivery of the same fact inserted first. The read above is the fast path; the unique index is the guarantee.
            _logger.LogDebug("Chat notifications of message {MessageId} were created by a concurrent delivery, skipping", source.MessageId);
            return new ChatNotificationDelivery(0, duplicates + delivered.Count, suppressed);
        }

        return new ChatNotificationDelivery(delivered.Count, duplicates, suppressed);
    }

    public static string ProducedType(string reason) => reason switch
    {
        ChatNotificationReasons.Mention => NotificationTypes.ChatMentionProducedType,
        ChatNotificationReasons.DirectMessage => NotificationTypes.ChatDirectMessageProducedType,
        ChatNotificationReasons.ThreadReply => NotificationTypes.ChatThreadReplyProducedType,
        ChatNotificationReasons.Broadcast => NotificationTypes.ChatBroadcastProducedType,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown chat notification reason."),
    };

    private static string Title(string reason) => reason switch
    {
        ChatNotificationReasons.Mention => "You were mentioned in chat",
        ChatNotificationReasons.DirectMessage => "New message in chat",
        ChatNotificationReasons.ThreadReply => "New reply in a thread you follow",
        _ => "Message to everyone in a channel",
    };

    private static string Key(ChatNotificationSource source, Guid userId, string reason) =>
        $"{ProducedType(reason)}:{source.MessageId:N}:{userId:N}";

    private static bool IsDeduplicationViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
            return postgres.ConstraintName == DeduplicationIndexName;

        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505", StringComparison.Ordinal) && message.Contains(DeduplicationIndexName, StringComparison.Ordinal);
    }
}
