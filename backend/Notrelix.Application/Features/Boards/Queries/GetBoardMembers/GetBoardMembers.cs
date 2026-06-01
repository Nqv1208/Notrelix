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

namespace Notrelix.Application.Features.Boards.Queries.GetBoardMembers;

public record GetBoardMembersQuery(Guid BoardId) : IRequest<Result<List<BoardMemberDto>>>;

public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, Result<List<BoardMemberDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardMembersQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<BoardMemberDto>>> Handle(GetBoardMembersQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == board.WorkspaceId && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var members = await _context.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Join(_context.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new BoardMemberDto(m.UserId, u.Name, u.AvatarUrl, m.Role.ToString(), m.JoinedAt))
            .ToListAsync(ct);

        return Result<List<BoardMemberDto>>.Success(members);
    }
}
