using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardLifecycleImpact;

/// <summary>
/// What archiving or deleting the board touches, read before the confirmation that names it. Authorized
/// as managing the board; a board the caller cannot manage is NotFound, exactly as a missing one.
/// </summary>
public record GetBoardLifecycleImpactQuery(Guid BoardId)
    : IQuery<Result<BoardLifecycleImpactDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardLifecycleImpactQueryHandler : IRequestHandler<GetBoardLifecycleImpactQuery, Result<BoardLifecycleImpactDto>>
{
    private readonly IWorkManagementDbContext _context;

    public GetBoardLifecycleImpactQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<BoardLifecycleImpactDto>> Handle(GetBoardLifecycleImpactQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var items = _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == board.Id && i.DeletedAt == null && !i.IsArchived);
        var itemCount = await items.CountAsync(ct);
        var openItemCount = await items.CountAsync(i => i.CompletedAt == null, ct);
        var memberCount = await _context.BoardMembers.CountAsync(m => m.BoardId == board.Id, ct);
        var groupCount = await _context.BoardGroups.CountAsync(g => g.BoardId == board.Id && g.DeletedAt == null, ct);
        var viewCount = await _context.BoardViews.CountAsync(v => v.BoardId == board.Id && !v.IsArchived, ct);

        return Result<BoardLifecycleImpactDto>.Success(new BoardLifecycleImpactDto(
            board.Id, board.IsArchived, itemCount, openItemCount, memberCount, groupCount, viewCount));
    }
}
