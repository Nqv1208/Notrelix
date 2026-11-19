using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Notifications.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <summary>
/// Tells the author of a message that a moderator removed it (UC-CHT-H03, CHT-019). The fact is the delete of a message; only a delete by
/// someone other than the author is a removal. The Application service decides at delivery from Chat's own rows and gives one intent to
/// Collaboration, which writes the Inbox notification once per message and recipient.
/// </summary>
[OrderingNotApplicable("A removal yields one notification guarded by a per-message and recipient deduplication key; it never supersedes another notification.")]
public sealed class ChatMessageRemovedNotificationConsumer(
    ChatNotificationHandoff handoff,
    ILogger<ChatMessageRemovedNotificationConsumer> logger) : IConsumer<ChatMessageDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ChatMessageDeletedIntegrationEvent> context)
    {
        var message = context.Message;
        if (message.DeletedBy == message.AuthorId)
            return;
        if (message.WorkspaceId is not { } workspaceId || workspaceId == Guid.Empty
            || message.AccountId is not { } accountId || accountId == Guid.Empty)
        {
            logger.LogWarning("chat.message.deleted {MessageId} has no authoritative Account/Workspace envelope, skipping the removal notice", message.MessageId);
            return;
        }

        await handoff.HandOffRemovalAsync(workspaceId, message.SpaceId, message.MessageId, message.EventId, DateTimeOffset.UtcNow, context.CancellationToken);
    }
}
