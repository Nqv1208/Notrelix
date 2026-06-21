using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;

public record GetBoardsQuery(Guid WorkspaceId) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId);
}

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, Result<List<BoardDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBoardsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == request.WorkspaceId && !b.IsArchived)
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
