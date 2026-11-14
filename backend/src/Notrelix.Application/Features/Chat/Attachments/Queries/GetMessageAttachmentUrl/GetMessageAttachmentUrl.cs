using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentUrl;

/// <summary>
/// Issues a pre-signed GET for one file of a message (UC-CHT-G08). The same authorization as the streaming content read runs on every
/// request: only a reader of the space gets a URL, the file must belong to exactly this message, and a deleted message is NotFound. The URL
/// lives for the configured download window only and is never cached or logged. Refused (409 storage-direct-download-unsupported) when
/// the storage provider cannot issue pre-signed URLs; the caller then streams the content.
/// </summary>
public record GetMessageAttachmentUrlQuery(Guid WorkspaceId, Guid MessageId, Guid AttachmentId)
    : IQuery<Result<MessageAttachmentUrlDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetMessageAttachmentUrlQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatMessageAttachmentTransfers transfers) : IRequestHandler<GetMessageAttachmentUrlQuery, Result<MessageAttachmentUrlDto>>
{
    public async Task<Result<MessageAttachmentUrlDto>> Handle(GetMessageAttachmentUrlQuery request, CancellationToken cancellationToken)
    {
        if (!transfers.SupportsPresignedUrls)
            throw new ConflictException("The storage provider does not issue download URLs.", "storage-direct-download-unsupported");

        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (message.IsDeleted)
            throw new NotFoundException(nameof(Message), request.MessageId);

        var link = await transfers.CreateDownloadUrlAsync(message.Id, request.AttachmentId, cancellationToken);
        return Result<MessageAttachmentUrlDto>.Success(new MessageAttachmentUrlDto(
            link.AttachmentId, link.FileName, link.ContentType, link.SizeBytes, link.Url, link.ExpiresAt));
    }
}
