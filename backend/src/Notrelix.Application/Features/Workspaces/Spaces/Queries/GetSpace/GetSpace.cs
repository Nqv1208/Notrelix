using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

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
) : IQuery<Result<SpaceDto>>, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceQueryHandler : IRequestHandler<GetSpaceQuery, Result<SpaceDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetSpaceQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SpaceDto>> Handle(GetSpaceQuery request, CancellationToken ct)
    {
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
            space.CreatedAt.DateTime));
    }
}
