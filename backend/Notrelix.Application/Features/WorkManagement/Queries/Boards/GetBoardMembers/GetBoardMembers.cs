using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoardMembers;

public record GetBoardMembersQuery(Guid WorkspaceId, Guid BoardId) : IRequest<Result<List<BoardMemberDto>>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Board;
    Guid IAuthorizeableRequest.ResourceId => BoardId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewBoard;
}

public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, Result<List<BoardMemberDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetBoardMembersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BoardMemberDto>>> Handle(GetBoardMembersQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var members = await _context.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Join(_context.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new BoardMemberDto(m.UserId, u.Name, u.AvatarUrl, m.Role.ToString(), m.JoinedAt.DateTime))
            .ToListAsync(ct);

        return Result<List<BoardMemberDto>>.Success(members);
    }
}
