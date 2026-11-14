using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Retention.Services;

/// <summary>What one batch did: messages tombstoned or purged, and threads purged with them.</summary>
public sealed record RetentionBatchResult(int Messages, int Threads)
{
    public static readonly RetentionBatchResult Empty = new(0, 0);
    public bool IsEmpty => Messages == 0;
}

/// <summary>An enabled retention rule the job works through.</summary>
public sealed record RetentionTarget(Guid AccountId, Guid WorkspaceId, int RetentionDays, RetentionMode Mode, Guid ConfiguredBy);

/// <summary>
/// Chat's retention work (UC-CHT-H05, CHT-021, CHT-018). The Infrastructure job finds the Workspaces with a rule, then, space by space,
/// calls <see cref="ApplyBatchAsync"/> in a transaction of its own until it returns empty. A batch selects expired user messages from the
/// state of the database, so a batch that failed is simply selected again by the next pass (it is idempotent), and a message that expired
/// but is already gone is not selected. One Audit fact is written per batch, with the counts, the space and the rule.
/// </summary>
/// <remarks>
/// A message is expired when it was created before now minus the window. System messages carry no one's words and are never removed.
/// Tombstone mode turns the message into a tombstone (the author, time and sequence stay) and removes its pin, reactions, mentions and
/// files. Purge mode deletes the row with the same things; a thread is purged as a whole, and only when its last reply is also past the
/// window, so a live conversation never loses its root. The sequence counter of the space is untouched, so a message sent later still takes
/// the next number and a reader sees a gap where purged messages were. Unread counts are counted from the rows that remain, so they stay
/// right. A space that is deleted (inside its recovery window, CHT-018) is not touched: the recovery hold.
/// One fact per batch tells the clients of the space to refetch (chat.space.changed), so a batch of 200 is not 200 frames.
/// </remarks>
public sealed class ChatRetentionApplier(
    IChatDbContext db,
    IChatMessageAttachments attachments,
    IChatSystemAudit audit,
    IDateTimeProvider clock)
{
    /// <summary>Every rule that has a window, so the job knows which Workspaces to work on.</summary>
    public async Task<IReadOnlyList<RetentionTarget>> ListTargetsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.ChatRetentionPolicies.AsNoTracking()
            .Where(p => p.RetentionDays != null)
            .OrderBy(p => p.WorkspaceId)
            .ToListAsync(cancellationToken);
        return rows
            .Select(p => new RetentionTarget(p.AccountId, p.WorkspaceId, p.RetentionDays!.Value, p.Mode, p.UpdatedBy ?? p.WorkspaceId))
            .ToList();
    }

    /// <summary>
    /// The live spaces of the Workspace that hold at least one message the rule would remove. A deleted space is not listed (recovery
    /// hold). Only the ids: the batch loads what it changes.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ListSpacesWithExpiredAsync(RetentionTarget target, CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow - TimeSpan.FromDays(target.RetentionDays);
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.WorkspaceId == target.WorkspaceId && s.DeletedAt == null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (live.Count == 0)
            return [];

        var tombstone = target.Mode == RetentionMode.Tombstone;
        var withExpired = await db.ChatMessages.AsNoTracking()
            .Where(m => m.WorkspaceId == target.WorkspaceId
                        && m.Kind == MessageKind.User
                        && m.CreatedAt < cutoff
                        && (!tombstone || m.DeletedAt == null))
            .Select(m => m.SpaceId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return live.Where(withExpired.Contains).Order().ToList();
    }

    /// <summary>
    /// Removes one batch (at most <see cref="ChatLimits.RetentionBatchSize"/> messages, a purged thread's replies counted in addition) from
    /// the space and writes its Audit fact. The caller saves and commits; nothing is written when the batch is empty or the space is held.
    /// </summary>
    public async Task<RetentionBatchResult> ApplyBatchAsync(RetentionTarget target, Guid spaceId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var cutoff = now - TimeSpan.FromDays(target.RetentionDays);

        var space = await db.ChatSpaces.FirstOrDefaultAsync(
            s => s.Id == spaceId && s.WorkspaceId == target.WorkspaceId, cancellationToken);
        if (space is null || space.DeletedAt is not null)
            return RetentionBatchResult.Empty; // recovery hold: a deleted space waits for its own purge

        var result = target.Mode == RetentionMode.Tombstone
            ? await TombstoneBatchAsync(target, spaceId, cutoff, now, cancellationToken)
            : await PurgeBatchAsync(target, spaceId, cutoff, cancellationToken);
        if (result.IsEmpty)
            return result;

        space.RecordRetentionApplied(now);
        await audit.RecordRetentionBatchAsync(
            new RetentionBatchFact(target.AccountId, target.WorkspaceId, spaceId, target.Mode.ToWire(), target.RetentionDays, cutoff, result.Messages, result.Threads),
            cancellationToken);
        return result;
    }

    private async Task<RetentionBatchResult> TombstoneBatchAsync(
        RetentionTarget target, Guid spaceId, DateTimeOffset cutoff, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var batch = await db.ChatMessages
            .Where(m => m.SpaceId == spaceId
                        && m.WorkspaceId == target.WorkspaceId
                        && m.Kind == MessageKind.User
                        && m.DeletedAt == null
                        && m.CreatedAt < cutoff)
            .OrderBy(m => m.Sequence)
            .Take(ChatLimits.RetentionBatchSize)
            .ToListAsync(cancellationToken);
        if (batch.Count == 0)
            return RetentionBatchResult.Empty;

        var ids = batch.Select(m => m.Id).ToList();
        foreach (var message in batch)
            message.ExpireByRetention(now);

        await RemoveAttachedAsync(ids, target.ConfiguredBy, cancellationToken);
        return new RetentionBatchResult(batch.Count, 0);
    }

    private async Task<RetentionBatchResult> PurgeBatchAsync(
        RetentionTarget target, Guid spaceId, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        // The units are messages of the main flow. A unit with a thread goes only when the thread's last reply is past the window too.
        var roots = await db.ChatMessages
            .Where(m => m.SpaceId == spaceId
                        && m.WorkspaceId == target.WorkspaceId
                        && m.Kind == MessageKind.User
                        && m.ThreadRootId == null
                        && m.CreatedAt < cutoff
                        && !db.ChatThreads.Any(t => t.RootMessageId == m.Id && t.LastReplyAt >= cutoff))
            .OrderBy(m => m.Sequence)
            .Take(ChatLimits.RetentionBatchSize)
            .ToListAsync(cancellationToken);
        if (roots.Count == 0)
            return RetentionBatchResult.Empty;

        var rootIds = roots.Select(m => m.Id).ToList();
        var replies = await db.ChatMessages
            .Where(m => m.SpaceId == spaceId && m.ThreadRootId != null && rootIds.Contains(m.ThreadRootId.Value))
            .ToListAsync(cancellationToken);
        var threads = await db.ChatThreads.Where(t => rootIds.Contains(t.RootMessageId)).ToListAsync(cancellationToken);
        var followers = await db.ChatThreadFollowers.Where(f => rootIds.Contains(f.RootMessageId)).ToListAsync(cancellationToken);

        var all = roots.Concat(replies).ToList();
        var ids = all.Select(m => m.Id).ToList();
        await RemoveAttachedAsync(ids, target.ConfiguredBy, cancellationToken);

        foreach (var follower in followers)
            db.ChatThreadFollowers.Remove(follower);
        foreach (var thread in threads)
            db.ChatThreads.Remove(thread);
        foreach (var message in all)
            db.ChatMessages.Remove(message);

        return new RetentionBatchResult(all.Count, threads.Count);
    }

    /// <summary>The pins, reactions, mentions and files that hang on the messages. No fact each: the batch has one.</summary>
    private async Task RemoveAttachedAsync(IReadOnlyCollection<Guid> messageIds, Guid deletedBy, CancellationToken cancellationToken)
    {
        foreach (var pin in await db.ChatPins.Where(p => messageIds.Contains(p.MessageId)).ToListAsync(cancellationToken))
            db.ChatPins.Remove(pin);
        foreach (var reaction in await db.ChatReactions.Where(r => messageIds.Contains(r.MessageId)).ToListAsync(cancellationToken))
            db.ChatReactions.Remove(reaction);
        foreach (var mention in await db.ChatMessageMentions.Where(m => messageIds.Contains(m.MessageId)).ToListAsync(cancellationToken))
            db.ChatMessageMentions.Remove(mention);

        // The files are soft deleted, so the attachment retention job removes the stored objects.
        await attachments.RemoveAllAsync(messageIds, deletedBy, cancellationToken);
    }
}
