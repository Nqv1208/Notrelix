using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <summary>
/// The per-user stream of a new message (UC-CHT-C09): every active member of the space except the author, who is not muted,
/// gets the new read state of the space: the unread number (sequence difference), the stored mention count and a preview of the
/// message. Cost per message: one query for the recipients (bounded by <see cref="ChatRealtime.MaxUnreadRecipients"/>, with their
/// stored boundary and mention count), then one realtime publish per recipient in chunks. Nothing is counted: the mention count
/// was raised by the send itself, in one UPDATE, before this runs. A muted, removed or left member gets no frame and no preview.
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
            .Select(m => new { m.UserId, m.ReadSequence, m.MentionCount, m.NotificationLevel, m.MutedUntil })
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

        var preview = new ChatRealtime.UnreadPreview(
            message.Sequence, message.AuthorId, message.Kind,
            message.Kind == MessageKindWire.UserKind ? ChatRealtime.PreviewText(message.Content) : null, message.OccurredAt);
        var frames = audience.Select(r => ChatRealtime.Unread(
            accountId, workspaceId, r.UserId, message.SpaceId,
            (int)Math.Min(Math.Max(message.Sequence - r.ReadSequence, 0), int.MaxValue), r.MentionCount,
            message.CorrelationId, message.EventId, now, message.Sequence, preview)).ToList();

        foreach (var chunk in frames.Chunk(PublishChunk))
        {
            await Task.WhenAll(chunk.Select(frame => _publisher.PublishAsync(frame, cancellationToken)));
        }
    }
}
