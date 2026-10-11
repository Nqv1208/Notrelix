using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.Common;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardsBySlug;

public record GetBoardsBySlugQuery(Guid WorkspaceId, string Slug) : IQuery<Result<List<BoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetBoardsBySlugQueryHandler : IRequestHandler<GetBoardsBySlugQuery, Result<List<BoardDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardsBySlugQueryHandler(
        IWorkManagementDbContext context,
        IWorkspaceAccessResolver workspaceAccess,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _workspaceAccess = workspaceAccess;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<BoardDto>>> Handle(GetBoardsBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _workspaceAccess.ResolveBySlugAsync(request.Slug, ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        // Same per-Board ViewBoard decision as a single read (GOV-029); a
        // principal without membership in the resolved Workspace sees nothing.
        var visible = await _visibility.VisibleBoardIdsAsync(workspace.Id, _requestContext.UserId, ct);
        var spaces = (await _visibility.VisibleSpaceIdsAsync(workspace.Id, _requestContext.UserId, ct)).ToHashSet();
        var result = await BoardListProjection.LoadAsync(_context, workspace.Id, visible, spaces, ct);

        return Result<List<BoardDto>>.Success(result);
    }
}
