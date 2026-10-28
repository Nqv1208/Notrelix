using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;

public record SpaceDto(
    Guid Id,
    string Name,
    string? Description,
    string Visibility,
    string SpaceType,
    bool IsArchived,
    DateTime CreatedAt
);

public record GetSpaceQuery(
    Guid WorkspaceId,
    Guid SpaceId
) : IQuery<Result<SpaceDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceQueryHandler : IRequestHandler<GetSpaceQuery, Result<SpaceDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetSpaceQueryHandler(IWorkspaceDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<SpaceDto>> Handle(GetSpaceQuery request, CancellationToken ct)
    {
        // A Space the caller cannot see is NotFound, not Forbidden: existence of a
        // Private Space is not disclosed (GOV-028/GOV-029).
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (!visible.Contains(request.SpaceId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var space = await _context.Spaces
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.WorkspaceId == request.WorkspaceId, ct);

        if (space is null)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        return Result<SpaceDto>.Success(new SpaceDto(
            space.Id,
            space.Name,
            space.Description,
            space.Visibility.ToString(),
            space.SpaceType.ToString(),
            space.Status == SpaceStatus.Archived,
            space.CreatedAt.UtcDateTime));
    }
}
