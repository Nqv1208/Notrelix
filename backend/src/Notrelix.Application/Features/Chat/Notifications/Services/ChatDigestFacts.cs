using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Public.Digest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Notifications.Services;

/// <summary>
/// Chat's answer to "which of these unread chat notifications still belong in this person's digest" (UC-CHT-H09). One query per table for
/// the whole set of candidates of a person. The rules are the delivery rules of <see cref="ChatNotificationHandoff"/> checked again now
/// (membership, mute and level) plus the read state: a message the person has read in Chat is not missed.
/// </summary>
public sealed class ChatDigestFacts(IChatDbContext db) : IChatDigestFacts
{
    public async Task<IReadOnlyList<ChatDigestFact>> DeliverableAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<ChatDigestCandidate> candidates,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return [];

        var messageIds = candidates.Select(c => c.MessageId).Distinct().ToList();
        var spaceIds = candidates.Select(c => c.SpaceId).Distinct().ToList();

        var messages = (await db.ChatMessages.AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && messageIds.Contains(m.Id) && m.DeletedAt == null)
                .ToListAsync(cancellationToken))
            .Where(m => !m.IsSystem && m.AuthorId != userId)
            .ToDictionary(m => m.Id);

        var spaces = (await db.ChatSpaces.AsNoTracking()
                .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null && spaceIds.Contains(s.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.Id);

        var memberships = (await db.ChatMemberships.AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.LeftAt == null && spaceIds.Contains(m.SpaceId))
                .ToListAsync(cancellationToken))
            .ToDictionary(m => m.SpaceId);

        var rootIds = messages.Values.Where(m => m.ThreadRootId is not null).Select(m => m.ThreadRootId!.Value).Distinct().ToList();
        var followers = rootIds.Count == 0
            ? new Dictionary<Guid, Domain.Chat.Messages.ThreadFollower>()
            : (await db.ChatThreadFollowers.AsNoTracking()
                    .Where(f => f.UserId == userId && rootIds.Contains(f.RootMessageId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(f => f.RootMessageId);

        var facts = new List<ChatDigestFact>();
        foreach (var candidate in candidates)
        {
            if (!messages.TryGetValue(candidate.MessageId, out var message)
                || message.SpaceId != candidate.SpaceId
                || !spaces.TryGetValue(candidate.SpaceId, out var space)
                || !memberships.TryGetValue(candidate.SpaceId, out var membership))
            {
                continue;
            }

            var level = Membership.EffectiveLevel(membership.NotificationLevel, membership.MutedUntil, now);
            if (level == NotificationLevel.None)
                continue;
            if (level == NotificationLevel.Mentions && candidate.Reason == ChatNotificationReasons.DirectMessage)
                continue;

            if (message.ThreadRootId is { } rootId)
            {
                followers.TryGetValue(rootId, out var follower);
                if (candidate.Reason == ChatNotificationReasons.ThreadReply && (follower is null || follower.Muted))
                    continue;
                var readReplies = follower?.ReadReplySequence ?? 0;
                if (message.ThreadSequence is { } replySequence && replySequence <= readReplies)
                    continue;
            }
            else if (message.Sequence <= membership.ReadSequence)
            {
                continue;
            }

            facts.Add(new ChatDigestFact(
                candidate.NotificationId,
                space.Id,
                space.IsConversation ? null : space.Name,
                message.Id,
                candidate.Reason,
                candidate.At,
                Shorten(message.Content),
                Path(workspaceId, space)));
        }

        return facts;
    }

    private static string? Shorten(string? content) =>
        content is null || content.Length <= IChatDigestFacts.PreviewMaxLength ? content : content[..IChatDigestFacts.PreviewMaxLength];

    private static string Path(Guid workspaceId, Space space) =>
        space.IsConversation ? $"/workspaces/{workspaceId}/chat/dm/{space.Id}" : $"/workspaces/{workspaceId}/chat/{space.Id}";
}
