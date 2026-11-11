using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <summary>
/// The per-user stream of a new message (UC-CHT-C09): every active member of the space except the author, who is not muted,
/// gets their new unread and mention count for the space. Cost per message: one query for the recipients (bounded by
/// <see cref="ChatRealtime.MaxUnreadRecipients"/>) and one grouped count for all of them, then one realtime publish per
/// recipient in chunks. Nothing is queried per recipient.
/// </summary>
public sealed class ChatUnreadFanOut
{
    private const int PublishChunk = 64;

    private readonly IChatDbContext _db;
    private readonly IRealtimePublisher _publisher;
    private readonly ILogger<ChatUnreadFanOut> _logger;

    public ChatUnreadFanOut(IChatDbContext db, IRealtimePublisher publisher, ILogger<ChatUnreadFanOut> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task NotifyCreatedAsync(ChatMessageCreatedIntegrationEvent message, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (message.WorkspaceId is not { } workspaceId || message.AccountId is not { } accountId)
            return;

        var recipients = await _db.ChatMemberships.AsNoTracking()
            .Where(m => m.SpaceId == message.SpaceId && m.LeftAt == null && m.UserId != message.AuthorId)
            .OrderBy(m => m.UserId)
            .Take(ChatRealtime.MaxUnreadRecipients + 1)
            .Select(m => new { m.UserId, m.ReadSequence, m.NotificationLevel, m.MutedUntil, m.MentionCount })
            .ToListAsync(cancellationToken);

        if (recipients.Count > ChatRealtime.MaxUnreadRecipients)
        {
            _logger.LogWarning(
                "Chat unread fan-out truncated at {Limit} recipients for space {SpaceId}",
                ChatRealtime.MaxUnreadRecipients, message.SpaceId);
            recipients = recipients.Take(ChatRealtime.MaxUnreadRecipients).ToList();
        }

        // Muted members get no badge and no frame (UC-CHT-C09, CHT-019).
        var audience = recipients
            .Where(r => Membership.EffectiveLevel(r.NotificationLevel, r.MutedUntil, now) != NotificationLevel.None)
            .ToList();
        if (audience.Count == 0)
            return;

        var userIds = audience.Select(r => r.UserId).ToHashSet();
        var unread = await (
            from m in _db.ChatMemberships.AsNoTracking()
            from msg in _db.ChatMessages.AsNoTracking()
            where m.SpaceId == message.SpaceId && m.LeftAt == null && m.UserId != message.AuthorId
                  && msg.SpaceId == m.SpaceId && msg.Sequence > m.ReadSequence
                  && msg.AuthorId != m.UserId && msg.Kind == MessageKind.User && msg.DeletedAt == null
            group msg by m.UserId into g
            select new { UserId = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var counts = unread.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId, u => u.Count);

        var frames = audience.Select(r => ChatRealtime.Unread(
            accountId, workspaceId, r.UserId, message.SpaceId,
            counts.GetValueOrDefault(r.UserId), r.MentionCount,
            message.CorrelationId, message.EventId, now)).ToList();

        foreach (var chunk in frames.Chunk(PublishChunk))
        {
            await Task.WhenAll(chunk.Select(frame => _publisher.PublishAsync(frame, cancellationToken)));
        }
    }
}
