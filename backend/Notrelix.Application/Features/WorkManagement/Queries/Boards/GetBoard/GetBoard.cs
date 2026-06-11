using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

using global::Notrelix.Application.Common.Security;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoard;

public record GetBoardQuery(Guid WorkspaceId, Guid BoardId) : IRequest<Result<BoardDto>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Board;
    Guid IAuthorizeableRequest.ResourceId => BoardId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewBoard;
}

public class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, Result<BoardDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBoardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BoardDto>> Handle(GetBoardQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var memberCount = await _context.BoardMembers.CountAsync(m => m.BoardId == board.Id, ct);
        var listCount = await _context.BoardGroups.CountAsync(l => l.BoardId == board.Id && !l.IsArchived, ct);

        return Result<BoardDto>.Success(new BoardDto(
            board.Id, board.WorkspaceId, board.Title, board.Description,
            board.Background, board.Visibility.ToString(), board.IsArchived,
            memberCount, listCount, board.CreatedAt
        ));
    }
}
