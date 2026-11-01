namespace Notrelix.Application.Features.WorkManagement.Transfer;

public record GetBoardExportQuery(Guid BoardId, Guid JobId)
    : IQuery<ExportJobDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<GetBoardExportQuery, ExportJobDto>
{
    public async Task<ExportJobDto> Handle(GetBoardExportQuery request, CancellationToken ct) =>
        ExportJobs.ToDto(await ExportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct), progress, clock);
}
