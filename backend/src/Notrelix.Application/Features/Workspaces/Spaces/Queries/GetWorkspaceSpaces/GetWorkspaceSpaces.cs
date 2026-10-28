using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;

public record GetWorkspaceSpacesQuery(
    Guid WorkspaceId
) : IQuery<Result<List<SpaceDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceSpacesQueryHandler : IRequestHandler<GetWorkspaceSpacesQuery, Result<List<SpaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceSpacesQueryHandler(IWorkspaceDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<SpaceDto>>> Handle(GetWorkspaceSpacesQuery request, CancellationToken ct)
    {
        // Private Spaces are not listed to principals who cannot see them (GOV-029).
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<List<SpaceDto>>.Success([]);

        var spaces = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Where(s => visible.Contains(s.Id))
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        var result = spaces.Select(s => new SpaceDto(
            s.Id,
            s.Name,
            s.Description,
            s.Visibility.ToString(),
            s.SpaceType.ToString(),
            s.Status == SpaceStatus.Archived,
            s.CreatedAt.UtcDateTime)).ToList();

        return Result<List<SpaceDto>>.Success(result);
    }
}
