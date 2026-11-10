using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;

/// <summary>
/// Sends a message to a space (UC-CHT-B01, B06). Only a member of the space can send, and an archived channel refuses
/// the send (409 channel-archived). A repeat with the same clientMessageId from the same author returns the stored
/// message and stores nothing (200). The sequence is assigned in the same transaction as the insert.
/// </summary>
[IdempotencyOperation("chat.messages.send-message.v1")]
public record SendMessageCommand(Guid WorkspaceId, Guid SpaceId, string Content, string ClientMessageId, Guid? ReplyToId)
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
    IDateTimeProvider clock) : IRequestHandler<SendMessageCommand, Result<SendMessageResultDto>>
{
    public async Task<Result<SendMessageResultDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), request.SpaceId);
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        // The lock comes before the duplicate check: two sends with one key then run one after the other, and the
        // second one finds the first one's row instead of storing a second message.
        await sequences.LockAsync(space.Id, cancellationToken);

        var clientMessageId = request.ClientMessageId.Trim();
        var stored = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.SpaceId == space.Id && m.AuthorId == userId && m.ClientMessageId == clientMessageId,
            cancellationToken);
        if (stored is not null)
            return Result<SendMessageResultDto>.Success(new SendMessageResultDto(MessageDto.From(stored), Created: false));

        if (request.ReplyToId is { } replyToId)
        {
            var quoted = await db.ChatMessages.AsNoTracking().AnyAsync(
                m => m.Id == replyToId && m.SpaceId == space.Id, cancellationToken);
            if (!quoted)
                throw new NotFoundException(nameof(Message), replyToId);
        }

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
        return Result<SendMessageResultDto>.Success(new SendMessageResultDto(MessageDto.From(message), Created: true));
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
