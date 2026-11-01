using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

public record GetBoardQuery(Guid BoardId) : IQuery<Result<BoardDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, Result<BoardDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardQueryHandler(IWorkManagementDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<BoardDto>> Handle(GetBoardQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var memberCount = await _context.BoardMembers.CountAsync(m => m.BoardId == board.Id, ct);
        var listCount = await _context.BoardGroups.CountAsync(l => l.BoardId == board.Id && l.DeletedAt == null && !l.IsArchived, ct);

        // A board reachable through an explicit grant may sit in a Space the
        // caller cannot see; that Space's id is not disclosed (GOV-029).
        Guid? spaceId = null;
        if (board.SpaceId is { } placedIn
            && (await _visibility.VisibleSpaceIdsAsync(board.WorkspaceId, _requestContext.UserId, ct)).Contains(placedIn))
        {
            spaceId = placedIn;
        }

        return Result<BoardDto>.Success(new BoardDto(
            board.Id, board.WorkspaceId, board.Title, board.Description,
            board.Background, board.Visibility.ToString(), board.IsArchived,
            memberCount, listCount, board.CreatedAt.UtcDateTime,
            board.Version,
            spaceId
        ));
    }
}
