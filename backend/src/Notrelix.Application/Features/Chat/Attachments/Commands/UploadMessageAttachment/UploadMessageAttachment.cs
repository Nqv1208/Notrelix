using System.Text.Json.Serialization;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;

using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Attachments.Commands.UploadMessageAttachment;

/// <summary>
/// Attaches a file to the caller's own message (UC-CHT-G08, CHT-012) through the shared attachment flow: the same storage path and file
/// policy (size cap, blocked extensions, sniffed type, per-message cap) as every other target, under the target kind chat.message.
/// Only an active member of the space attaches, and only to their own message (403 for another's message); a Guest only in a space they
/// were added to. A space the caller cannot see is 404. An archived channel is 409 channel-archived; a system or deleted message is 409.
/// A blocked type is refused before any byte is stored (415) and a file over the cap while it streams (413). The standing of the caller,
/// the channel and the message is read again under the space lock after the bytes are stored, so a leave, an archive or a delete that
/// committed during a long upload refuses it and the stored object is removed with the failed request.
/// </summary>
[IdempotencyOperation("chat.attachments.upload-message-attachment.v1")]
public record UploadMessageAttachmentCommand(
    Guid WorkspaceId,
    Guid MessageId,
    string FileName,
    [property: JsonIgnore, IdempotencyFingerprintIgnore] Stream Content)
    : ICommand<Result<MessageAttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UploadMessageAttachmentCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ISpaceSequenceAllocator sequences,
    IChatMessageAttachments attachments,
    IDateTimeProvider clock) : IRequestHandler<UploadMessageAttachmentCommand, Result<MessageAttachmentDto>>
{
    public async Task<Result<MessageAttachmentDto>> Handle(UploadMessageAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        // Tracked on purpose: the message raises the file fact (no column changes), which reaches the channel stream.
        var message = await db.ChatMessages.FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (message.AuthorId != userId)
            throw new ForbiddenException("Only the author can attach a file to this message.");
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);
        message.EnsureAttachable();
        // A channel that turns attaching off refuses before any byte is stored, and again under the lock below.
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.Attach, cancellationToken);

        // The bytes stream before any lock (a long upload must not hold the space). The file policy runs inside the shared flow.
        var stored = await attachments.UploadAsync(message.Id, request.FileName, request.Content, cancellationToken);

        // The row is in the unit of work but not committed. Standing is read again under the space lock: a leave, an archive or a
        // delete that committed during the upload fails this request, and the compensation registered by the shared flow removes the object.
        await sequences.LockAsync(space.Id, cancellationToken);
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
        await ChannelPermissionChecks.RequireAsync(db, space, caller, userId, ChannelAction.Attach, cancellationToken);

        var current = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);
        current.EnsureAttachable();

        message.RecordAttachmentAdded(
            userId, stored.Id, stored.FileName, stored.SizeBytes, stored.ContentType, stored.UploadedBy, stored.CreatedAt, clock.UtcNow);

        return Result<MessageAttachmentDto>.Success(MessageAttachments.ToDto(request.WorkspaceId, stored));
    }
}

public class UploadMessageAttachmentCommandValidator : AbstractValidator<UploadMessageAttachmentCommand>
{
    public UploadMessageAttachmentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Content).NotNull();
    }
}
