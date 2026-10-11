using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;

/// <summary>
/// One node of the caller's Space tree. <paramref name="ParentSpaceId"/> is null
/// for a root Space and also when the parent is a Space the caller cannot see:
/// such a Space is presented as a root and the hidden parent is never disclosed
/// (GOV-029). Siblings are ordered by <paramref name="Position"/> (ordinal).
/// </summary>
/// <param name="Version">The Space aggregate version, sent back as <c>expectedVersion</c> by move and membership writes.</param>
public record SpaceTreeNodeDto(
    Guid Id,
    Guid? ParentSpaceId,
    string Name,
    string? Description,
    string SpaceType,
    string Visibility,
    bool IsArchived,
    string Position,
    long Version,
    DateTimeOffset CreatedAt);

/// <summary>
/// The Workspace's Space org tree as a flat, parent-linked list, restricted to
/// the Spaces the caller may see (single-read Space visibility, GOV-029).
/// </summary>
public record GetSpaceTreeQuery(Guid WorkspaceId)
    : IQuery<Result<List<SpaceTreeNodeDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceTreeQueryHandler : IRequestHandler<GetSpaceTreeQuery, Result<List<SpaceTreeNodeDto>>>
{
    private readonly ISpaceStore _spaces;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetSpaceTreeQueryHandler(ISpaceStore spaces, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _spaces = spaces;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<List<SpaceTreeNodeDto>>> Handle(GetSpaceTreeQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0)
            return Result<List<SpaceTreeNodeDto>>.Success([]);

        var rows = await _spaces.ListTreeAsync(request.WorkspaceId, visible, ct);
        var visibleIds = rows.Select(r => r.Id).ToHashSet();

        var nodes = rows
            .OrderBy(r => r.ParentSpaceId)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.Id)
            .Select(r => new SpaceTreeNodeDto(
                r.Id,
                r.ParentSpaceId is { } parentId && visibleIds.Contains(parentId) ? parentId : null,
                r.Name,
                r.Description,
                r.SpaceType.ToString(),
                r.Visibility.ToString(),
                r.Status == SpaceStatus.Archived,
                r.Position.Value,
                r.Version,
                r.CreatedAt))
            .ToList();

        return Result<List<SpaceTreeNodeDto>>.Success(nodes);
    }
}
