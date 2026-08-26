using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
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

    public GetWorkspaceSpacesQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<SpaceDto>>> Handle(GetWorkspaceSpacesQuery request, CancellationToken ct)
    {
        var spaces = await _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == request.WorkspaceId && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        var result = spaces.Select(s => new SpaceDto(
            s.Id,
            s.Name,
            s.Description,
            s.Visibility.ToString(),
            s.SpaceType.ToString(),
            s.Status == SpaceStatus.Archived,
            s.CreatedAt.DateTime)).ToList();

        return Result<List<SpaceDto>>.Success(result);
    }
}
