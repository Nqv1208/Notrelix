namespace Notrelix.Application.Features.Chat.Abstractions;

/// <summary>
/// Moves the caller's read boundaries in the database (CHT-009). Each call is one UPDATE that takes the membership row
/// lock and sets the boundary to GREATEST(current, target), so a boundary never moves backwards, even when two requests
/// race. Only the caller's own active memberships are touched. The stored mention count of a membership (UC-CHT-D04) moves
/// in the same statements: a new mention adds one, a deleted or edited mention takes one away, and a read sets it to the
/// mentions still after the new boundary.
/// </summary>
public interface IChatReadStateStore
{
    /// <summary>Moves the boundary in one space to the target when it is ahead. Returns false when the caller has no active membership there.</summary>
    Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the boundary in every live space of the Workspace to that space's last sequence (UC-CHT-D07). The statement sees
    /// the sequences committed when it starts, so a message sent during the call stays unread. Returns the rows it touched.
    /// </summary>
    Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// A new message was stored at <paramref name="sequence"/>: the author's own boundary moves to it when the author had read everything before it,
    /// so a person's own message is never their unread (one row, no count).
    /// </summary>
    Task AdvanceAuthorAsync(Guid workspaceId, Guid spaceId, Guid authorId, long sequence, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one to the mention count of each member the audience reaches who has not read <paramref name="sequence"/> and was a member when
    /// the message was sent. One UPDATE for all of them; the author is never counted.
    /// </summary>
    Task AddMentionsAsync(Guid workspaceId, Guid spaceId, long sequence, DateTimeOffset createdAt, Guid authorId, MentionAudience audience, CancellationToken cancellationToken);

    /// <summary>
    /// Takes the stored mentions of these messages off the mention counts of the members who still count them (a delete, an edit that
    /// replaces them, a retention purge). Call it before the mention rows are removed.
    /// </summary>
    Task RemoveMentionsAsync(Guid workspaceId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);
}

/// <summary>Who a message's mentions reach: every member (an @channel or @here) or the named people.</summary>
public readonly record struct MentionAudience(bool Everyone, IReadOnlyList<Guid> Users)
{
    public bool IsEmpty => !Everyone && Users.Count == 0;

    public static MentionAudience From(IEnumerable<Notrelix.Domain.Chat.Messages.MentionSpan> spans)
    {
        var list = spans.ToList();
        return new MentionAudience(
            list.Any(s => s.Kind is Notrelix.Domain.Chat.Messages.MentionKind.BroadcastChannel or Notrelix.Domain.Chat.Messages.MentionKind.BroadcastHere),
            list.Where(s => s.Kind == Notrelix.Domain.Chat.Messages.MentionKind.User && s.TargetId is not null)
                .Select(s => s.TargetId!.Value).Distinct().ToList());
    }
}
