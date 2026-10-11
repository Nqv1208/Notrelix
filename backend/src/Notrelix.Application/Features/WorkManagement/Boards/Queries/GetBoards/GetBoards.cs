using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.Common;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;

public record GetBoardsQuery(Guid WorkspaceId) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, Result<List<BoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardsQueryHandler(
        IWorkManagementDbContext context,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsQuery request, CancellationToken ct)
    {
        // The pipeline authorized ViewWorkspace; each Board still needs the
        // single-read ViewBoard decision, applied in the datastore (GOV-029).
        var visible = await _visibility.VisibleBoardIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        var result = await BoardListProjection.LoadAsync(_context, request.WorkspaceId, visible, ct);
        return Result<List<BoardDto>>.Success(result);
    }
}
