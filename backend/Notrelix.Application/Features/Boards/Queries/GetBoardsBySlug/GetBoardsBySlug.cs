using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Boards.Queries.GetBoardsBySlug;

public record GetBoardsBySlugQuery(string Slug) : IRequest<Result<List<BoardDto>>>;

public class GetBoardsBySlugQueryHandler : IRequestHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardsBySlugQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var boards = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == workspace.Id && !b.IsArchived)
            .Select(b => new BoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Description,
                b.Background, b.Visibility.ToString(), b.IsArchived,
                _context.BoardMembers.Count(m => m.BoardId == b.Id),
                _context.BoardLists.Count(l => l.BoardId == b.Id && !l.IsArchived),
                b.CreatedAt
            )).ToListAsync(ct);

        return Result<List<BoardDto>>.Success(boards);
    }
}
