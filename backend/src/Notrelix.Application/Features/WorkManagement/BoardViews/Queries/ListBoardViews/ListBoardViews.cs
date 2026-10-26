using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Queries.ListBoardViews;

/// <summary>
/// The saved views the caller sees on a Board: every active Board view and the caller's own private ones,
/// the default first, then oldest first (WM-8). A private view of another member is not listed.
/// </summary>
public record ListBoardViewsQuery(Guid BoardId)
    : IQuery<Result<IReadOnlyList<BoardViewDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardViewsQueryHandler : IRequestHandler<ListBoardViewsQuery, Result<IReadOnlyList<BoardViewDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public ListBoardViewsQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<IReadOnlyList<BoardViewDto>>> Handle(ListBoardViewsQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var views = await _context.BoardViews
            .AsNoTracking()
            .Where(v => v.BoardId == request.BoardId
                        && v.DeletedAt == null
                        && !v.IsArchived
                        && (v.Visibility == BoardViewVisibility.Board || v.CreatedBy == userId))
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.CreatedAt)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);

        return Result<IReadOnlyList<BoardViewDto>>.Success(views.Select(BoardViewDto.From).ToList());
    }
}
