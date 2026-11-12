using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Attachments.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments;

/// <summary>
/// The files of one message, oldest first (UC-CHT-G08). Only a reader of the space sees them: a private space the caller is not a member
/// of, or a Guest outside the spaces they were added to, is the same NotFound as a missing message. A tombstone has no files.
/// </summary>
public record ListMessageAttachmentsQuery(Guid WorkspaceId, Guid MessageId)
    : IQuery<Result<IReadOnlyList<MessageAttachmentDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListMessageAttachmentsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatMessageAttachments attachments) : IRequestHandler<ListMessageAttachmentsQuery, Result<IReadOnlyList<MessageAttachmentDto>>>
{
    public async Task<Result<IReadOnlyList<MessageAttachmentDto>>> Handle(ListMessageAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        return Result<IReadOnlyList<MessageAttachmentDto>>.Success(
            await MessageAttachments.ForMessageAsync(attachments, request.WorkspaceId, message.Id, cancellationToken));
    }
}
