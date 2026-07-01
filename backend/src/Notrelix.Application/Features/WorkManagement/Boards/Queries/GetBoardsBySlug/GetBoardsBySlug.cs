using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardsBySlug;

public record GetBoardsBySlugQuery(Guid WorkspaceId, string Slug) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId);
}

public class GetBoardsBySlugQueryHandler : IRequestHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBoardsBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == workspace.Id && !b.IsArchived)
            .ToListAsync(ct);

        var result = boards.Select(b => new BoardDto(
            b.Id, b.WorkspaceId, b.Title, b.Description,
            b.Background, b.Visibility.ToString(), b.IsArchived,
            _context.BoardMembers.Count(m => m.BoardId == b.Id),
            _context.BoardGroups.Count(l => l.BoardId == b.Id && !l.IsDeleted),
            b.CreatedAt.DateTime
        )).ToList();

        return Result<List<BoardDto>>.Success(result);
    }
}
