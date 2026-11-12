using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Attachments.Commands.DeleteMessageAttachment;

/// <summary>
/// Removes one file from the caller's own message (UC-CHT-G08). Only the author removes it, and only while they are an active member of
/// an unarchived channel; a file that is not a file of this message is the same NotFound as a missing one. The stored object is purged
/// by the attachment retention job, not here.
/// </summary>
public record DeleteMessageAttachmentCommand(Guid WorkspaceId, Guid MessageId, Guid AttachmentId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeleteMessageAttachmentCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatMessageAttachments attachments) : IRequestHandler<DeleteMessageAttachmentCommand, Result>
{
    public async Task<Result> Handle(DeleteMessageAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), message.SpaceId);
        if (message.AuthorId != userId)
            throw new ForbiddenException("Only the author can remove a file from this message.");
        if (space.IsArchived)
            throw new ConflictException("The channel is archived and read-only.", ChatRuleCodes.ChannelArchived);

        if (!await attachments.RemoveAsync(message.Id, request.AttachmentId, cancellationToken))
            throw new NotFoundException("Attachment", request.AttachmentId);
        return Result.Success();
    }
}

public class DeleteMessageAttachmentCommandValidator : AbstractValidator<DeleteMessageAttachmentCommand>
{
    public DeleteMessageAttachmentCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}
