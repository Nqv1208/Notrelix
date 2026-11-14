using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Notifications.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <summary>
/// Hands the notification intents of a new chat message to the Inbox (UC-CHT-G09, CHT-019). The Application service maps the fact to
/// intents from Chat's own rows at delivery time (recipient still a member, space not archived, not muted, not the author) and gives
/// them to Collaboration, which applies preferences and writes the notification once per message, recipient and reason. A fact without an
/// Account and Workspace envelope has no tenant to notify in and is ignored.
/// </summary>
[OrderingNotApplicable("Each message yields independent notifications guarded by a per-message, recipient and reason deduplication key; notifications never supersede one another, and the rules are evaluated against the current membership at delivery.")]
public sealed class ChatNotificationIntentConsumer : IConsumer<ChatMessageCreatedIntegrationEvent>
{
    private readonly ChatNotificationHandoff _handoff;
    private readonly ILogger<ChatNotificationIntentConsumer> _logger;

    public ChatNotificationIntentConsumer(ChatNotificationHandoff handoff, ILogger<ChatNotificationIntentConsumer> logger)
    {
        _handoff = handoff;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ChatMessageCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (message.WorkspaceId is not { } workspaceId || workspaceId == Guid.Empty
            || message.AccountId is not { } accountId || accountId == Guid.Empty)
        {
            _logger.LogWarning("chat.message.created {MessageId} has no authoritative Account/Workspace envelope, skipping notifications", message.MessageId);
            return;
        }

        var delivery = await _handoff.HandOffAsync(workspaceId, message.MessageId, message.EventId, DateTimeOffset.UtcNow, context.CancellationToken);
        _logger.LogDebug(
            "Chat notifications for message {MessageId}: {Delivered} delivered, {Duplicates} already there, {Suppressed} left out by preference",
            message.MessageId, delivery.Delivered, delivery.Duplicates, delivery.Suppressed);
    }
}
