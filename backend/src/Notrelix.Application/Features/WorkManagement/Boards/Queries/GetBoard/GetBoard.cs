using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

public record GetBoardQuery(Guid WorkspaceId, Guid BoardId) : IQuery<Result<BoardDto>>, IRequirePermission, IWorkspaceRequest, ICacheableQuery<Result<BoardDto>>
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public string CacheKey => $"board:{BoardId}";
    public TimeSpan? Ttl => null;
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
        var listCount = await _context.BoardGroups.CountAsync(l => l.BoardId == board.Id && l.DeletedAt == null, ct);

        return Result<BoardDto>.Success(new BoardDto(
            board.Id, board.WorkspaceId, board.Title, board.Description,
            board.Background, board.Visibility.ToString(), board.IsArchived,
            memberCount, listCount, board.CreatedAt.DateTime
        ));
    }
}
