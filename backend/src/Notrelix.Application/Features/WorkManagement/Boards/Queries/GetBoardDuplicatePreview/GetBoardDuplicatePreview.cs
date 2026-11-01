using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.DuplicateBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardDuplicatePreview;

/// <summary>
/// What copying the Board with its items would do, read before the copy: how many root items, whether that is over
/// the limit, and how many assignments would be dropped because the person has no access to the copy.
/// </summary>
public record GetBoardDuplicatePreviewQuery(Guid BoardId, Guid? SpaceId)
    : IQuery<Result<BoardDuplicatePreviewDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public sealed record BoardDuplicatePreviewDto(int ItemCount, int MaxItems, bool OverLimit, int AssigneeCount, int DroppedAssignees);

public class GetBoardDuplicatePreviewQueryHandler : IRequestHandler<GetBoardDuplicatePreviewQuery, Result<BoardDuplicatePreviewDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly IBoardSharingSummary _sharing;
    private readonly IWorkspaceAccessResolver _access;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardDuplicatePreviewQueryHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        IBoardSharingSummary sharing,
        IWorkspaceAccessResolver access,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _placement = placement;
        _sharing = sharing;
        _access = access;
        _requestContext = requestContext;
    }

    public async Task<Result<BoardDuplicatePreviewDto>> Handle(GetBoardDuplicatePreviewQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        await _placement.AuthorizeTargetAsync(board.WorkspaceId, request.SpaceId, ct);

        var itemIds = await _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == board.Id && i.ParentItemId == null && !i.IsArchived)
            .Select(i => i.Id)
            .ToListAsync(ct);
        var assigned = itemIds.Count == 0 || itemIds.Count > BoardTemplateStructure.MaxItems
            ? []
            : await _context.BoardItemMembers.AsNoTracking()
                .Where(m => itemIds.Contains(m.ItemId))
                .Select(m => new { m.ItemId, m.UserId })
                .Distinct()
                .ToListAsync(ct);
        var keepers = await DuplicateAudience.KeepersAsync(
            _sharing, _access, board.WorkspaceId, request.SpaceId, _requestContext.UserId, assigned.Select(a => a.UserId), ct);

        return Result<BoardDuplicatePreviewDto>.Success(new BoardDuplicatePreviewDto(
            itemIds.Count,
            BoardTemplateStructure.MaxItems,
            itemIds.Count > BoardTemplateStructure.MaxItems,
            assigned.Select(a => a.UserId).Distinct().Count(),
            assigned.Count(a => !keepers.Contains(a.UserId))));
    }
}
