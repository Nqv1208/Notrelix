using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Notifications.Services;

/// <summary>
/// Maps a chat fact to notification intents and hands them to the Inbox (UC-CHT-G09, CHT-019, CHT-010). The facts are a mention of a
/// person, a message in a direct or group conversation, a reply in a thread the person follows and an @channel broadcast. The rules are
/// decided here, at delivery, from Chat's own rows (never from the event's age): the recipient must still be an active member of a
/// live, not archived space (a person who left, was removed or whose Workspace membership ended is not notified), a muted space gives
/// nothing, a space at the Mentions level gives only mentions, broadcasts and followed threads, the author is never a recipient, and a
/// deleted or system message gives nothing. One recipient gets one notification per message: the most specific reason wins
/// (mention, thread reply, direct message, broadcast). Collaboration then applies the recipient's own preferences and writes the Inbox
/// notification once per (message, recipient, reason).
/// </summary>
/// <remarks>
/// Fan-out is bounded by <see cref="MaxRecipients"/> (the unread fan-out bound): a broadcast or conversation above it reaches the first
/// ones by user id and logs the cut, and a mention or a followed thread always reaches the person named. @here reaches the channel members who
/// are in the realtime presence set when the message is handed off (online, not idle), never a Guest and never a person who is not a
/// member; the unread badge counts it for everyone.
/// The preview is the message text (Collaboration shortens it); only a recipient who is a member sees it.
/// </remarks>
public sealed class ChatNotificationHandoff(
    IChatDbContext db,
    IChatNotificationIntents intents,
    IRealtimePresenceRegistry presence,
    IWorkspaceMemberFacts workspaceMembers,
    ILogger<ChatNotificationHandoff> logger)
{
    public const int MaxRecipients = ChatRealtime.MaxUnreadRecipients;

    public async Task<ChatNotificationDelivery> HandOffAsync(
        Guid workspaceId, Guid messageId, Guid? sourceEventId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == messageId && m.WorkspaceId == workspaceId, cancellationToken);
        if (message is null || message.IsDeleted || message.IsSystem)
            return ChatNotificationDelivery.None;

        var space = await db.ChatSpaces.AsNoTracking().FirstOrDefaultAsync(
            s => s.Id == message.SpaceId && s.WorkspaceId == workspaceId && s.DeletedAt == null, cancellationToken);
        if (space is null || space.IsArchived)
            return ChatNotificationDelivery.None;

        var mentions = await db.ChatMessageMentions.AsNoTracking()
            .Where(x => x.MessageId == message.Id)
            .Select(x => new { x.Kind, x.TargetId })
            .ToListAsync(cancellationToken);
        var mentioned = mentions
            .Where(x => x.Kind == MentionKind.User && x.TargetId is not null)
            .Select(x => x.TargetId!.Value)
            .Distinct()
            .ToList();
        var broadcast = mentions.Any(x => x.Kind == MentionKind.BroadcastChannel);
        // @here reaches the members who are online now (the realtime presence set at hand-off); a Guest is never one of them.
        var here = !broadcast && mentions.Any(x => x.Kind == MentionKind.BroadcastHere)
            ? await OnlineNonGuestsAsync(workspaceId, message.AuthorId, cancellationToken)
            : [];

        var followers = message.ThreadRootId is { } rootId
            ? await db.ChatThreadFollowers.AsNoTracking()
                .Where(f => f.RootMessageId == rootId && !f.Muted)
                .OrderBy(f => f.UserId)
                .Select(f => f.UserId)
                .Take(MaxRecipients + 1)
                .ToListAsync(cancellationToken)
            : [];
        if (followers.Count > MaxRecipients)
        {
            logger.LogWarning("Chat thread notification fan-out truncated at {Limit} followers for message {MessageId}", MaxRecipients, message.Id);
            followers = followers.Take(MaxRecipients).ToList();
        }

        var sendsToConversation = space.IsConversation && !message.IsThreadReply;
        var everyone = broadcast || sendsToConversation;

        var members = await db.ChatMemberships.AsNoTracking()
            .Where(m => m.SpaceId == space.Id && m.LeftAt == null && m.UserId != message.AuthorId)
            .Where(m => everyone || mentioned.Contains(m.UserId) || followers.Contains(m.UserId) || here.Contains(m.UserId))
            .OrderBy(m => m.UserId)
            .Take(MaxRecipients + 1)
            .Select(m => new { m.UserId, m.NotificationLevel, m.MutedUntil })
            .ToListAsync(cancellationToken);
        if (members.Count > MaxRecipients)
        {
            logger.LogWarning("Chat notification fan-out truncated at {Limit} recipients for space {SpaceId}", MaxRecipients, space.Id);
            members = members.Take(MaxRecipients).ToList();
        }

        var mentionedSet = mentioned.ToHashSet();
        var followerSet = followers.ToHashSet();
        var intentsToHand = new List<ChatNotificationIntent>();
        foreach (var member in members)
        {
            var level = Membership.EffectiveLevel(member.NotificationLevel, member.MutedUntil, now);
            var reason = ReasonFor(
                level,
                mentionedSet.Contains(member.UserId),
                followerSet.Contains(member.UserId),
                sendsToConversation,
                broadcast || here.Contains(member.UserId));
            if (reason is not null)
                intentsToHand.Add(new ChatNotificationIntent(member.UserId, reason));
        }

        if (intentsToHand.Count == 0)
            return ChatNotificationDelivery.None;

        var source = new ChatNotificationSource(
            message.AccountId,
            message.WorkspaceId,
            space.Id,
            message.Id,
            message.ThreadRootId,
            message.AuthorId,
            sourceEventId,
            SpaceName(space),
            message.Content,
            now);
        return await intents.DeliverAsync(source, intentsToHand, cancellationToken);
    }

    /// <summary>
    /// The one reason a recipient is notified, or null for none. Muted (level None) gives nothing for any reason. At the Mentions level a
    /// message in a conversation is not enough; a mention, a broadcast and a followed thread still are.
    /// </summary>
    public static string? ReasonFor(NotificationLevel level, bool mentioned, bool followsThread, bool conversationMessage, bool broadcast)
    {
        if (level == NotificationLevel.None)
            return null;

        if (mentioned)
            return ChatNotificationReasons.Mention;
        if (followsThread)
            return ChatNotificationReasons.ThreadReply;
        if (conversationMessage && level == NotificationLevel.All)
            return ChatNotificationReasons.DirectMessage;
        if (broadcast)
            return ChatNotificationReasons.Broadcast;
        return null;
    }

    private async Task<List<Guid>> OnlineNonGuestsAsync(Guid workspaceId, Guid authorId, CancellationToken cancellationToken)
    {
        var online = new List<Guid>();
        foreach (var userId in presence.OnlineUserIds(workspaceId).Where(u => u != authorId).Order().Take(MaxRecipients))
        {
            var fact = await workspaceMembers.FindAsync(workspaceId, userId, cancellationToken);
            if (fact is { IsActive: true } && !ChannelAccess.IsGuest(fact))
                online.Add(userId);
        }

        return online;
    }

    private static string? SpaceName(Space space) => space.IsConversation ? null : space.Name;
}
