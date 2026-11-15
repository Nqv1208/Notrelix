using System.Reflection;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>
/// The rules of <c>ChatReadStateStore</c> in memory, over the test doubles of the chat tables: the boundary never moves back, a read
/// sets the mention count to the mentions still after the boundary, a send adds one for each member the mentions reach, and a delete
/// takes the stored mentions off. The SQL form runs against Postgres in the integration tests.
/// </summary>
internal sealed class InMemoryChatReadStateStore(IChatDbContext db) : IChatReadStateStore
{
    private static readonly PropertyInfo MentionCountProperty = typeof(Membership).GetProperty(nameof(Membership.MentionCount))!;

    private static void SetMentions(Membership m, int value) => MentionCountProperty.SetValue(m, Math.Max(0, value));

    private List<MessageMention> Mentions => db.ChatMessageMentions.ToList();

    private bool Counts(Message msg, Membership m, List<MessageMention> mentions, bool beingDeleted = false) =>
        msg.SpaceId == m.SpaceId && msg.Sequence > m.ReadSequence && msg.CreatedAt >= m.JoinedAt && msg.AuthorId != m.UserId
        && msg.Kind == MessageKind.User && (msg.DeletedAt == null || beingDeleted)
        && mentions.Any(x => x.MessageId == msg.Id
            && ((x.Kind == MentionKind.User && x.TargetId == m.UserId) || x.Kind is MentionKind.BroadcastChannel or MentionKind.BroadcastHere));

    private void Recount(Membership m)
    {
        var mentions = Mentions;
        SetMentions(m, db.ChatMessages.ToList().Count(msg => Counts(msg, m, mentions)));
    }

    public Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken)
    {
        var membership = db.ChatMemberships.ToList().SingleOrDefault(m => m.SpaceId == spaceId && m.UserId == userId && m.IsActive);
        if (membership is null) return Task.FromResult(false);

        membership.AdvanceReadSequence(target);
        Recount(membership);
        return Task.FromResult(true);
    }

    public Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var touched = 0;
        var spaces = db.ChatSpaces.ToList().Where(s => s.WorkspaceId == workspaceId && !s.IsDeleted).ToList();
        foreach (var membership in db.ChatMemberships.ToList().Where(m => m.UserId == userId && m.IsActive))
        {
            var space = spaces.SingleOrDefault(s => s.Id == membership.SpaceId);
            if (space is null) continue;
            membership.AdvanceReadSequence(space.LastSequence);
            Recount(membership);
            touched++;
        }
        return Task.FromResult(touched);
    }

    public Task AdvanceAuthorAsync(Guid workspaceId, Guid spaceId, Guid authorId, long sequence, CancellationToken cancellationToken)
    {
        var membership = db.ChatMemberships.ToList().SingleOrDefault(m => m.SpaceId == spaceId && m.UserId == authorId);
        if (membership is not null && membership.ReadSequence == sequence - 1)
            membership.AdvanceReadSequence(sequence);
        return Task.CompletedTask;
    }

    public Task AddMentionsAsync(
        Guid workspaceId, Guid spaceId, long sequence, DateTimeOffset createdAt, Guid authorId, MentionAudience audience, CancellationToken cancellationToken)
    {
        foreach (var m in db.ChatMemberships.ToList().Where(m =>
                     m.SpaceId == spaceId && m.IsActive && m.UserId != authorId && m.ReadSequence < sequence && m.JoinedAt <= createdAt
                     && (audience.Everyone || audience.Users.Contains(m.UserId))))
            SetMentions(m, m.MentionCount + 1);
        return Task.CompletedTask;
    }

    public Task RemoveMentionsAsync(Guid workspaceId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        var mentions = Mentions;
        var messages = db.ChatMessages.ToList().Where(msg => messageIds.Contains(msg.Id)).ToList();
        foreach (var m in db.ChatMemberships.ToList().Where(m => m.IsActive))
            SetMentions(m, m.MentionCount - messages.Count(msg => Counts(msg, m, mentions, beingDeleted: true)));
        return Task.CompletedTask;
    }
}
