using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
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

namespace Notrelix.Application.Features.Boards.Queries.GetBoard;

public record GetBoardQuery(Guid BoardId) : IRequest<Result<BoardDto>>;

public class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, Result<BoardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<BoardDto>> Handle(GetBoardQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == board.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var memberCount = await _context.BoardMembers.CountAsync(m => m.BoardId == board.Id, ct);
        var listCount = await _context.BoardLists.CountAsync(l => l.BoardId == board.Id && !l.IsArchived, ct);

        return Result<BoardDto>.Success(new BoardDto(
            board.Id, board.WorkspaceId, board.Title, board.Description,
            board.Background, board.Visibility.ToString(), board.IsArchived,
            memberCount, listCount, board.CreatedAt
        ));
    }
}
