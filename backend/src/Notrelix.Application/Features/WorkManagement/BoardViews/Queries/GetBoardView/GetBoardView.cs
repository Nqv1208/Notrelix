using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Queries.GetBoardView;

public record GetBoardViewQuery(Guid BoardId) : IQuery<Result<object>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardViewQueryHandler : IRequestHandler<GetBoardViewQuery, Result<object>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardViewQueryHandler(IWorkManagementDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<object>> Handle(GetBoardViewQuery request, CancellationToken ct)
    {
        var view = await _context.BoardViews.AsNoTracking()
            .Where(v => v.BoardId == request.BoardId && v.DeletedAt == null && v.Visibility == BoardViewVisibility.Board)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (view is null)
            return Result<object>.Success(new { type = "Table", config = "{}" });

        return Result<object>.Success(new { type = view.Type.ToString(), config = view.Config.Data.Value });
    }
}
