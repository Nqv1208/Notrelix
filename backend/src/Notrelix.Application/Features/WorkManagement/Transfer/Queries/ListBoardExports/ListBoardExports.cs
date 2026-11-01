namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>The caller's exports of this board from the last 24 hours, newest first.</summary>
public record ListBoardExportsQuery(Guid BoardId)
    : IQuery<IReadOnlyList<ExportJobDto>>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class ListBoardExportsQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me, IDateTimeProvider clock)
    : IRequestHandler<ListBoardExportsQuery, IReadOnlyList<ExportJobDto>>
{
    public async Task<IReadOnlyList<ExportJobDto>> Handle(ListBoardExportsQuery request, CancellationToken ct) =>
        (await jobs.ListExportsAsync(request.BoardId, me.UserId, clock.UtcNow - ExportJobs.FileLife, ct))
            .Select(j => ExportJobs.ToDto(j, progress, clock)).ToList();
}
