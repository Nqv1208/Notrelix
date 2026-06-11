using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Security;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetBoardView;

public record GetBoardViewQuery(Guid WorkspaceId, Guid BoardId) : IRequest<Result<object>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Board;
    Guid IAuthorizeableRequest.ResourceId => BoardId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.ViewBoard;
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
            .FirstOrDefaultAsync(v => v.BoardId == request.BoardId && v.UserId == _currentUser.UserId, ct);

        if (view is null)
            return Result<object>.Success(new { viewMode = "Table", filters = "{}", config = "{}" });

        return Result<object>.Success(new { viewMode = view.ViewMode.ToString(), filters = view.Filters, config = view.Filters });
    }
}
