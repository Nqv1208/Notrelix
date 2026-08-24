using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardsBySlug;

public record GetBoardsBySlugQuery(Guid WorkspaceId, string Slug) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetBoardsBySlugQueryHandler : IRequestHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public GetBoardsBySlugQueryHandler(IWorkManagementDbContext context, IWorkspaceAccessResolver workspaceAccess)
    {
        _context = context;
        _workspaceAccess = workspaceAccess;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _workspaceAccess.ResolveBySlugAsync(request.Slug, ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == workspace.Id && !b.IsArchived)
            .ToListAsync(ct);

        var boardIds = boards.Select(b => b.Id).ToList();

        var memberCounts = await _context.BoardMembers.AsNoTracking()
            .Where(m => boardIds.Contains(m.BoardId))
            .GroupBy(m => m.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count, ct);

        var groupCounts = await _context.BoardGroups.AsNoTracking()
            .Where(l => boardIds.Contains(l.BoardId) && !l.IsDeleted)
            .GroupBy(l => l.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count, ct);

        var result = boards.Select(b => new BoardDto(
            b.Id, b.WorkspaceId, b.Title, b.Description,
            b.Background, b.Visibility.ToString(), b.IsArchived,
            memberCounts.GetValueOrDefault(b.Id),
            groupCounts.GetValueOrDefault(b.Id),
            b.CreatedAt.DateTime
        )).ToList();

        return Result<List<BoardDto>>.Success(result);
    }
}
