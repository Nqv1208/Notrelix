using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Mentions.Services;
using Notrelix.Application.Features.Chat.Conversations.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;

/// <summary>
/// Replies in the thread of a root message (UC-CHT-F01). Only an active member of the space replies, under the same rules
/// as a send: an archived channel refuses it (409 channel-archived), a repeat with the same clientMessageId returns the
/// stored reply (200), and a deleted root with no replies is refused (409 message-deleted). A reply to a reply or to a
/// system message is refused (409 thread-root-invalid). The reply takes the next space sequence and the next thread
/// sequence under the space lock. It reopens an archived thread, and the replier and the root's author follow it.
/// With AlsoSendToChannel the reply also shows in the channel's main flow. Mentions are resolved as in a send (UC-CHT-G01 to G04).
/// </summary>
[IdempotencyOperation("chat.threads.reply-in-thread.v1")]
public record ReplyInThreadCommand(
    Guid WorkspaceId,
    Guid RootMessageId,
    string Content,
    string ClientMessageId,
    bool AlsoSendToChannel,
    IReadOnlyList<MentionInput>? Mentions = null,
    bool ConfirmBroadcast = false)
    : ICommand<Result<SendMessageResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ReplyInThreadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock,
    IBroadcastAuthorizer broadcast) : IRequestHandler<ReplyInThreadCommand, Result<SendMessageResultDto>>
{
    public async Task<Result<SendMessageResultDto>> Handle(ReplyInThreadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var root = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.RootMessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.RootMessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, root.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), root.SpaceId);
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        if (root.IsThreadReply || root.IsSystem)
            throw new ConflictException("Only a message of the main flow can have replies.", ChatRuleCodes.ThreadRootInvalid);

        // The lock comes first, as in a send: every reply and send to this space runs one after the other, so the
        // thread's count and the space sequence cannot be taken twice.
        await sequences.LockAsync(space.Id, cancellationToken);

        // Standing is read again after the lock: a leave, a removal, an archive or a delete that committed while this
        // request waited is respected.
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Select(s => new { Archived = s.ArchivedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Space), root.SpaceId);
        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), root.SpaceId);
        if (live.Archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        var clientMessageId = request.ClientMessageId.Trim();
        var stored = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.SpaceId == space.Id && m.AuthorId == userId && m.ClientMessageId == clientMessageId,
            cancellationToken);
        if (stored is not null)
        {
            var storedMentions = (await db.ChatMessageMentions.AsNoTracking()
                .Where(m => m.MessageId == stored.Id).OrderBy(m => m.Start).ToListAsync(cancellationToken))
                .Select(m => m.ToSpan()).ToList();
            var again = new MentionResolution(storedMentions, MentionResolver.StoredWarnings(space, storedMentions));
            return Result<SendMessageResultDto>.Success(new SendMessageResultDto(
                await ReplyPreviews.DtoAsync(db, request.WorkspaceId, userId, stored, cancellationToken, again), Created: false));
        }

        // The root is read again under the lock: a delete that committed while this request waited is respected.
        var rootDeleted = await db.ChatMessages.AsNoTracking()
            .Where(m => m.Id == root.Id)
            .Select(m => m.DeletedAt != null)
            .FirstOrDefaultAsync(cancellationToken);

        var thread = await db.ChatThreads.FirstOrDefaultAsync(t => t.RootMessageId == root.Id, cancellationToken);
        if (rootDeleted && (thread?.ReplyCount ?? 0) == 0)
            throw new ConflictException("The message was deleted and has no replies.", ChatRuleCodes.MessageDeleted);

        var resolution = await MentionResolver.ResolveAsync(
            db, members, broadcast, space, caller, request.Content, request.Mentions, request.ConfirmBroadcast, cancellationToken);

        var now = clock.UtcNow;
        if (thread is null)
        {
            thread = MessageThread.Open(accountId, request.WorkspaceId, space.Id, root.Id, now);
            db.ChatThreads.Add(thread);
        }

        var sequence = await sequences.NextAsync(space.Id, cancellationToken);
        var threadSequence = thread.RecordReply(now);
        var reply = Message.CreateThreadReply(
            accountId,
            request.WorkspaceId,
            space.Id,
            sequence,
            root.Id,
            threadSequence,
            userId,
            request.Content,
            clientMessageId,
            request.AlsoSendToChannel,
            now);
        db.ChatMessages.Add(reply);
        await MessageMentions.ReplaceAsync(db, reply, resolution.Spans, hadBefore: false, cancellationToken);
        // A new message brings a hidden conversation back to every list that hid it (UC-CHT-E07).
        ConversationVisibility.ShowAgain(space);

        // The root's author follows the thread by default when they still belong to the space (CHT-013). The replier
        // follows it too and has read their own reply.
        // When the replier is the root's author there is one follow row, ensured once with the replier's pointer.
        if (root.AuthorId != userId && space.IsActiveMember(root.AuthorId))
            await EnsureFollowerAsync(db, accountId, request.WorkspaceId, root.Id, root.AuthorId, readReplySequence: 0, cancellationToken);
        await EnsureFollowerAsync(db, accountId, request.WorkspaceId, root.Id, userId, readReplySequence: threadSequence, cancellationToken);

        return Result<SendMessageResultDto>.Success(new SendMessageResultDto(
            await ReplyPreviews.DtoAsync(db, request.WorkspaceId, userId, reply, cancellationToken, resolution), Created: true));
    }

    /// <summary>Adds a default follow (not muted) when the person has no follow row yet. An existing row is never overwritten.</summary>
    internal static async Task EnsureFollowerAsync(
        IChatDbContext db, Guid accountId, Guid workspaceId, Guid rootMessageId, Guid userId, long readReplySequence, CancellationToken ct)
    {
        var follower = await db.ChatThreadFollowers.FirstOrDefaultAsync(
            f => f.RootMessageId == rootMessageId && f.UserId == userId, ct);
        if (follower is null)
        {
            db.ChatThreadFollowers.Add(ThreadFollower.Follow(accountId, workspaceId, rootMessageId, userId, muted: false, readReplySequence));
            return;
        }

        if (readReplySequence > 0)
            follower.MarkReadThrough(readReplySequence);
    }
}

public class ReplyInThreadCommandValidator : AbstractValidator<ReplyInThreadCommand>
{
    public ReplyInThreadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.RootMessageId).NotEmpty();
        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content is not null && content.Trim().Length is >= 1 and <= ChatLimits.MessageMaxLength)
            .WithMessage($"A message has 1 to {ChatLimits.MessageMaxLength} characters after trim.");
        RuleFor(x => x.ClientMessageId)
            .NotNull()
            .Must(id => id is not null && id.Trim().Length is >= 1 and <= ChatLimits.ClientMessageIdMaxLength)
            .WithMessage($"clientMessageId is required and has at most {ChatLimits.ClientMessageIdMaxLength} characters.");
    }
}
