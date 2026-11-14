using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Exports.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;

namespace Notrelix.Application.Features.Chat.Exports.Queries.GetChatExport;

/// <summary>
/// The state of an export (UC-CHT-H06): queued, running, ready, failed or expired, with the counts. Only the person who asked reads it; an
/// export of someone else, or one that does not exist, is the same NotFound.
/// </summary>
public record GetChatExportQuery(Guid WorkspaceId, Guid ExportId)
    : IQuery<Result<ChatExportDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetChatExportQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<GetChatExportQuery, Result<ChatExportDto>>
{
    public async Task<Result<ChatExportDto>> Handle(GetChatExportQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var job = await db.ChatExportJobs.AsNoTracking().FirstOrDefaultAsync(
            j => j.Id == request.ExportId && j.WorkspaceId == request.WorkspaceId && j.RequestedBy == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChatExportJob), request.ExportId);
        return Result<ChatExportDto>.Success(ChatExportDto.From(job, clock.UtcNow));
    }
}
