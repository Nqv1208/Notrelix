using Notrelix.Application.Common.Storage;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>How many items an export would hold, for the dialog. Guests are refused here too.</summary>
public record GetBoardExportSummaryQuery(Guid BoardId, string? Filter, string? Sort)
    : IQuery<ExportSummaryDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardExportSummaryQueryHandler(
    IWorkManagementDbContext context, BoardExportBuilder builder, IWorkspaceAccessResolver access, ICurrentRequestContext me)
    : IRequestHandler<GetBoardExportSummaryQuery, ExportSummaryDto>
{
    public async Task<ExportSummaryDto> Handle(GetBoardExportSummaryQuery request, CancellationToken ct)
    {
        var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(Board), request.BoardId);
        await ExportJobs.RequireMemberAsync(access, board.WorkspaceId, me.UserId, ct);
        var (view, all) = await builder.CountAsync(request.BoardId, request.Filter, request.Sort, ct);
        return new ExportSummaryDto(view, all, []);
    }
}
