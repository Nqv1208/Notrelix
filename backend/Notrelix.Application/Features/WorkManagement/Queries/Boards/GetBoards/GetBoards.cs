using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoards;

public record GetBoardsQuery(Guid WorkspaceId) : IRequest<Result<List<BoardDto>>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewWorkspace;
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
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == request.WorkspaceId && !b.IsArchived)
            .Select(b => new BoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Description,
                b.Background, b.Visibility.ToString(), b.IsArchived,
                _context.BoardMembers.Count(m => m.BoardId == b.Id),
                _context.BoardGroups.Count(l => l.BoardId == b.Id && !l.IsArchived),
                b.CreatedAt
            )).ToListAsync(ct);

        return Result<List<BoardDto>>.Success(boards);
    }
}
