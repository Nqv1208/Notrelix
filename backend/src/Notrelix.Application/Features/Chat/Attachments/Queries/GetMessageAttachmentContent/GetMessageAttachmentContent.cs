using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent;

/// <summary>
/// Opens the stored bytes of one file of a message (UC-CHT-G08, CHT-012). The download is permission checked on every request: only a
/// reader of the space opens it, and the file must belong to exactly this message, so an attachment id of another message or target is
/// NotFound. A deleted message and a removed file are NotFound. The consumer disposes the stream.
/// </summary>
public record GetMessageAttachmentContentQuery(Guid WorkspaceId, Guid MessageId, Guid AttachmentId)
    : IQuery<Result<MessageAttachmentContentDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetMessageAttachmentContentQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatMessageAttachments attachments) : IRequestHandler<GetMessageAttachmentContentQuery, Result<MessageAttachmentContentDto>>
{
    public async Task<Result<MessageAttachmentContentDto>> Handle(GetMessageAttachmentContentQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (message.IsDeleted)
            throw new NotFoundException(nameof(Message), request.MessageId);

        var content = await attachments.OpenAsync(message.Id, request.AttachmentId, cancellationToken);
        return Result<MessageAttachmentContentDto>.Success(
            new MessageAttachmentContentDto(content.Content, content.FileName, content.ContentType, content.SizeBytes));
    }
}
