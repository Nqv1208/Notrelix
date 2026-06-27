using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;

public record GetBoardsQuery(Guid WorkspaceId) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId);
}

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, Result<List<BoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IWorkspaceAccessChecker _workspaceAccessChecker;

    public GetBoardsQueryHandler(IWorkManagementDbContext context, IWorkspaceAccessChecker workspaceAccessChecker)
    {
        _context = context;
        _workspaceAccessChecker = workspaceAccessChecker;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsQuery request, CancellationToken ct)
    {
        var workspaceCheck = await _workspaceAccessChecker.EnsureWorkspaceIsActiveAsync(request.WorkspaceId, ct);
        if (!workspaceCheck.Succeeded)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

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
