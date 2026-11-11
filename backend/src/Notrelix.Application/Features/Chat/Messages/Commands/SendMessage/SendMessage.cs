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

namespace Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;

/// <summary>
/// Sends a message to a space (UC-CHT-B01, B06). Only a member of the space can send, and an archived channel refuses
/// the send (409 channel-archived). A repeat with the same clientMessageId from the same author returns the stored
/// message and stores nothing (200). The sequence is assigned in the same transaction as the insert.
/// Mentions are validated here against the space, never trusted from the client (CHT-010, COL-030, UC-CHT-G01 to G04): see
/// <see cref="MentionResolver"/>. ConfirmBroadcast acknowledges an @channel or @here in a channel above the member threshold.
/// </summary>
[IdempotencyOperation("chat.messages.send-message.v1")]
public record SendMessageCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    string Content,
    string ClientMessageId,
    Guid? ReplyToId,
    IReadOnlyList<MentionInput>? Mentions = null,
    bool ConfirmBroadcast = false)
    : ICommand<Result<SendMessageResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SendMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock,
    IBroadcastAuthorizer broadcast) : IRequestHandler<SendMessageCommand, Result<SendMessageResultDto>>
{
    public async Task<Result<SendMessageResultDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), request.SpaceId);
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        // The lock comes before the duplicate check: two sends with one key then run one after the other, and the
        // second one finds the first one's row instead of storing a second message.
        await sequences.LockAsync(space.Id, cancellationToken);

        // The checks above ran on rows read before the lock. A leave, a removal, an archive or a delete that held the
        // lock first has committed by now, so the standing of the channel and of the caller is read again from the
        // database before anything is stored.
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Select(s => new { Archived = s.ArchivedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);
        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), request.SpaceId);
        if (live.Archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        var clientMessageId = request.ClientMessageId.Trim();
        var stored = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.SpaceId == space.Id && m.AuthorId == userId && m.ClientMessageId == clientMessageId,
            cancellationToken);
        if (stored is not null)
        {
            // A retry returns the stored message and the warnings its stored mentions give now; nothing is stored again.
            var storedMentions = (await db.ChatMessageMentions.AsNoTracking()
                .Where(m => m.MessageId == stored.Id).OrderBy(m => m.Start).ToListAsync(cancellationToken))
                .Select(m => m.ToSpan()).ToList();
            var again = new MentionResolution(storedMentions, MentionResolver.StoredWarnings(space, storedMentions));
            return Result<SendMessageResultDto>.Success(new SendMessageResultDto(
                await ReplyPreviews.DtoAsync(db, request.WorkspaceId, userId, stored, cancellationToken, again), Created: false));
        }

        if (request.ReplyToId is { } replyToId)
        {
            var quoted = await db.ChatMessages.AsNoTracking().AnyAsync(
                m => m.Id == replyToId && m.SpaceId == space.Id, cancellationToken);
            if (!quoted)
                throw new NotFoundException(nameof(Message), replyToId);
        }

        var resolution = await MentionResolver.ResolveAsync(
            db, members, broadcast, space, caller, request.Content, request.Mentions, request.ConfirmBroadcast, cancellationToken);

        var sequence = await sequences.NextAsync(space.Id, cancellationToken);
        var message = Message.CreateUserMessage(
            accountId,
            request.WorkspaceId,
            space.Id,
            sequence,
            userId,
            request.Content,
            clientMessageId,
            request.ReplyToId,
            clock.UtcNow);

        db.ChatMessages.Add(message);
        await MessageMentions.ReplaceAsync(db, message, resolution.Spans, hadBefore: false, cancellationToken);
        // A new message brings a hidden conversation back to every list that hid it (UC-CHT-E07).
        ConversationVisibility.ShowAgain(space);
        return Result<SendMessageResultDto>.Success(new SendMessageResultDto(
            await ReplyPreviews.DtoAsync(db, request.WorkspaceId, userId, message, cancellationToken, resolution), Created: true));
    }
}

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
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
