using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Pins.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;

using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Pins.Commands.PinMessage;

/// <summary>
/// Pins a message in its space (UC-CHT-F06). Only an active member of the space pins, under the pin action; a Guest is refused
/// by the action. A system message or a deleted message is refused (409). A space holds at most <see cref="ChatLimits.PinLimit"/>
/// pins; the next one is refused with 422 limit-reached. The count and the insert run under the space lock, so two pins at the
/// limit cannot both succeed. Pinning a message that is already pinned returns the pin (200) and changes nothing. A new pin
/// announces itself in the channel with a system message (201).
/// </summary>
public record PinMessageCommand(Guid WorkspaceId, Guid MessageId)
    : ICommand<Result<PinResultDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePin;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class PinMessageCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    ChatSystemMessages systemMessages,
    IDateTimeProvider clock,
    IChatMessageAttachments attachments) : IRequestHandler<PinMessageCommand, Result<PinResultDto>>
{
    public async Task<Result<PinResultDto>> Handle(PinMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        // The lock comes first, as in a send and a delete: the count, the duplicate check and the insert run one at a time
        // per space, so the limit holds under concurrency.
        await sequences.LockAsync(space.Id, cancellationToken);

        // Standing and the message are read again after the lock: a leave, an archive or a delete that committed while this
        // request waited is respected.
        var live = await db.ChatSpaces.AsNoTracking()
            .Where(s => s.Id == space.Id && s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Select(s => new { Archived = s.ArchivedAt != null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Space), message.SpaceId);
        var stillMember = await db.ChatMemberships.AsNoTracking()
            .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.LeftAt == null, cancellationToken);
        if (!stillMember)
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (live.Archived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.Pin, cancellationToken);

        var current = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);
        current.EnsurePinnable();

        var existing = await db.ChatPins.AsNoTracking().FirstOrDefaultAsync(p => p.MessageId == current.Id, cancellationToken);
        if (existing is not null)
        {
            var existingDto = new PinDto(
                await ReplyPreviews.DtoAsync(db, attachments, request.WorkspaceId, userId, ChannelAccess.IsGuest(caller), current, cancellationToken),
                existing.PinnedBy,
                existing.PinnedAt);
            return Result<PinResultDto>.Success(new PinResultDto(existingDto, Created: false));
        }

        var pinsInSpace = await db.ChatPins.CountAsync(p => p.SpaceId == space.Id, cancellationToken);
        Pin.EnsureRoom(pinsInSpace);

        var now = clock.UtcNow;
        var pin = Pin.Create(accountId, request.WorkspaceId, space.Id, current.Id, userId, now);
        db.ChatPins.Add(pin);
        await systemMessages.AnnounceAsync(space, userId, ChatSystemMessages.MessagePinned, cancellationToken);

        var dto = new PinDto(await ReplyPreviews.DtoAsync(db, attachments, request.WorkspaceId, userId, ChannelAccess.IsGuest(caller), current, cancellationToken), userId, now);
        return Result<PinResultDto>.Success(new PinResultDto(dto, Created: true));
    }
}

public class PinMessageCommandValidator : AbstractValidator<PinMessageCommand>
{
    public PinMessageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
