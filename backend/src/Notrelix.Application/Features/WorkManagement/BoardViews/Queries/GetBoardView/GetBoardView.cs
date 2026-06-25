using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Queries.GetBoardView;

public record GetBoardViewQuery(Guid WorkspaceId, Guid BoardId) : IQuery<Result<object>>, IRequirePermission, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
}

public class GetBoardViewQueryHandler : IRequestHandler<GetBoardViewQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetBoardViewQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<object>> Handle(GetBoardViewQuery request, CancellationToken ct)
    {
        var view = await _context.BoardViews.AsNoTracking()
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId && v.CreatedBy == _currentUser.UserId, ct);

        if (view is null)
            return Result<object>.Success(new { type = "Table", config = "{}" });

        return Result<object>.Success(new { type = view.Type.ToString(), config = view.Config.Data.Value });
    }
}
