using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.ListDeletedBoards;

/// <summary>
/// The boards of one Workspace deleted inside the 30-day restore window that the caller can restore:
/// the ones the caller deleted and the ones where the caller is a board Owner or Admin. A deleted board
/// is NotFound to everyone else, so it is absent here, never marked. A deleter who has since lost the
/// board's Space and holds no role on the board no longer sees it; a deleted board at the Workspace root
/// stays listed for its deleter while it is visible to the whole Workspace. Restore itself is authorized again
/// on the board (ManageBoard).
/// </summary>
public record ListDeletedBoardsQuery(Guid WorkspaceId)
    : IQuery<Result<List<RetiredBoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListDeletedBoardsQueryHandler : IRequestHandler<ListDeletedBoardsQuery, Result<List<RetiredBoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public ListDeletedBoardsQueryHandler(
        IWorkManagementDbContext context,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<List<RetiredBoardDto>>> Handle(ListDeletedBoardsQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var accountId = _requestContext.RequireAccountId();
        var since = _clock.UtcNow - Board.RestoreWindow;
        var managerRoles = new[] { BoardRole.Owner, BoardRole.Admin };
        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, userId, ct)).ToList();

        // The global filter hides soft-deleted rows, and bypassing it also drops its tenant predicate,
        // so the resolved tenant scope is applied here.
        var boards = await _context.Boards.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.AccountId == accountId && b.WorkspaceId == request.WorkspaceId
                && b.DeletedAt != null && b.DeletedAt >= since
                && ((b.DeletedBy == userId
                        && ((b.SpaceId != null && spaces.Contains(b.SpaceId.Value))
                            || (b.SpaceId == null && b.Visibility == BoardVisibility.Workspace)
                            || _context.BoardMembers.Any(m => m.BoardId == b.Id && m.UserId == userId)))
                    || _context.BoardMembers.Any(m => m.BoardId == b.Id && m.UserId == userId && managerRoles.Contains(m.Role))))
            .OrderByDescending(b => b.DeletedAt)
            .ToListAsync(ct);

        return Result<List<RetiredBoardDto>>.Success(boards
            .Select(b => new RetiredBoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Background, b.Visibility.ToString(), b.Version,
                b.SpaceId is { } spaceId && spaces.Contains(spaceId) ? spaceId : null,
                string.IsNullOrWhiteSpace(b.ItemKeyPrefix) ? null : b.ItemKeyPrefix,
                b.DeletedAt!.Value.UtcDateTime,
                (b.DeletedAt.Value + Board.RestoreWindow).UtcDateTime))
            .ToList());
    }
}
