using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Relations.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Relations.Queries.ListBoardRelations;

public record ListBoardRelationsQuery(Guid BoardId)
    : IQuery<Result<List<BoardRelationDto>>>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardRelationsQueryHandler : IRequestHandler<ListBoardRelationsQuery, Result<List<BoardRelationDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListBoardRelationsQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<List<BoardRelationDto>>> Handle(ListBoardRelationsQuery request, CancellationToken ct)
    {
        var relations = await _context.BoardRelations.AsNoTracking()
            .Where(r => r.SourceBoardId == request.BoardId || r.TargetBoardId == request.BoardId)
            .Select(r => new BoardRelationDto(
                r.Id,
                r.SourceBoardId,
                r.TargetBoardId,
                r.RelationType.ToString(),
                r.Direction.ToString(),
                r.SyncMode.ToString(),
                r.Status.ToString()))
            .ToListAsync(ct);

        return Result<List<BoardRelationDto>>.Success(relations);
    }
}
