using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Exports.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;

namespace Notrelix.Application.Features.Chat.Exports.Queries.GetChatExportContent;

/// <summary>
/// The file of a ready export (UC-CHT-H06): authorized on every call (the requester must still be an Owner or Admin of the Workspace), for the
/// person who asked only, and never after its expiry. The bytes are streamed by the API; there is no URL that works without this check. An
/// export that is not ready, not yours or past its expiry is NotFound.
/// </summary>
public record GetChatExportContentQuery(Guid WorkspaceId, Guid ExportId)
    : IQuery<ChatExportContentDto>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetChatExportContentQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IStorageService storage,
    IDateTimeProvider clock) : IRequestHandler<GetChatExportContentQuery, ChatExportContentDto>
{
    public async Task<ChatExportContentDto> Handle(GetChatExportContentQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var job = await db.ChatExportJobs.AsNoTracking().FirstOrDefaultAsync(
            j => j.Id == request.ExportId && j.WorkspaceId == request.WorkspaceId && j.RequestedBy == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatExportJob), request.ExportId);
        if (!job.IsDownloadableAt(clock.UtcNow))
            throw new NotFoundException("ChatExportFile", request.ExportId);

        var content = await storage.OpenReadAsync(job.StorageKey!, cancellationToken)
            ?? throw new NotFoundException("ChatExportFile", request.ExportId);
        return new ChatExportContentDto(content, ChatExportFiles.FileName(job), ChatExportFiles.ContentType, job.SizeBytes);
    }
}
