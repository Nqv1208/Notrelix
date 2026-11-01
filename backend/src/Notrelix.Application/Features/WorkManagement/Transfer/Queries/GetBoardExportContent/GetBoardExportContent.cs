using Notrelix.Application.Common.Storage;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>The file of a ready export, for the person who asked: authorised on every call, never after its expiry.</summary>
public record GetBoardExportContentQuery(Guid BoardId, Guid JobId)
    : IQuery<ExportContentDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportContentQueryHandler(
    IBoardTransferJobs jobs, IStorageService storage, IWorkspaceAccessResolver access, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<GetBoardExportContentQuery, ExportContentDto>
{
    public async Task<ExportContentDto> Handle(GetBoardExportContentQuery request, CancellationToken ct)
    {
        var job = await ExportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        await ExportJobs.RequireMemberAsync(access, job.WorkspaceId, me.UserId, ct);
        if (job.Status != TransferStatus.Ready || job.StorageKey is null || job.ExpiresAt <= clock.UtcNow)
            throw new NotFoundException("BoardExportFile", request.JobId);
        var content = await storage.OpenReadAsync(job.StorageKey, ct) ?? throw new NotFoundException("BoardExportFile", request.JobId);
        var type = job.Format == TabularWriter.Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv; charset=utf-8";
        return new ExportContentDto(content, job.Options<ExportOptions>().FileName, type, job.Detail<ExportOutcome>()?.Size ?? 0);
    }
}
