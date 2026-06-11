using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoardsBySlug;

public record GetBoardsBySlugQuery(Guid WorkspaceId, string Slug) : IRequest<Result<List<BoardDto>>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewWorkspace;
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
