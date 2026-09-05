using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;

namespace Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements;

/// <summary>
/// Read model for one Analytics placement row.
/// </summary>
public sealed record WorkItemPlacementDto(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid GroupId,
    bool IsArchived,
    long SourceRevision,
    DateTimeOffset LastOccurredAt);

/// <summary>
/// Analytics-owned query over the local placement projection. Reads only the
/// derived Analytics state — no Work Management source query occurs on the
/// normal read path. Not exposed through HTTP; the reference proves the
/// projection is consumable.
/// </summary>
public record GetWorkspacePlacementsQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<WorkItemPlacementDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource =>
        ResourceRef.Create(ResourceKind.Create("analytics.placement"), WorkspaceId, WorkspaceId);
}

public class GetWorkspacePlacementsQueryHandler
    : IRequestHandler<GetWorkspacePlacementsQuery, Result<IReadOnlyList<WorkItemPlacementDto>>>
{
    private readonly IReportingDbContext _context;

    public GetWorkspacePlacementsQueryHandler(IReportingDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<WorkItemPlacementDto>>> Handle(
        GetWorkspacePlacementsQuery request,
        CancellationToken cancellationToken)
    {
        var placements = await _context.WorkspaceWorkItemPlacements
            .AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId)
            .OrderBy(p => p.BoardId).ThenBy(p => p.LastOccurredAt)
            .Select(p => new WorkItemPlacementDto(
                p.WorkspaceId,
                p.ItemId,
                p.BoardId,
                p.GroupId,
                p.IsArchived,
                p.SourceRevision,
                p.LastOccurredAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WorkItemPlacementDto>>.Success(placements);
    }
}
