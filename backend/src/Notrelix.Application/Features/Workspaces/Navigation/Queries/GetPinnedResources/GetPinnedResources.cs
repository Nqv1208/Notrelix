using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;

/// <summary>
/// WX-BE-002: the current member's personal pins in a Workspace, ordered by pin
/// time (newest first). Reordering is deferred. References only (WX-DEC-011).
/// </summary>
public record GetPinnedResourcesQuery(
    Guid WorkspaceId
) : IQuery<Result<IReadOnlyList<PinnedResourceDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public record PinnedResourceDto(string Kind, Guid ResourceId, DateTimeOffset PinnedAt);

public class GetPinnedResourcesQueryHandler : IRequestHandler<GetPinnedResourcesQuery, Result<IReadOnlyList<PinnedResourceDto>>>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IResourceVisibility _visibility;

    public GetPinnedResourcesQueryHandler(
        IWorkspaceNavigationStore store,
        ICurrentRequestContext requestContext,
        IResourceVisibility visibility)
    {
        _visibility = visibility;
        _store = store;
        _requestContext = requestContext;
    }

    public async Task<Result<IReadOnlyList<PinnedResourceDto>>> Handle(GetPinnedResourcesQuery request, CancellationToken ct)
    {
        var entries = await _store.ListPinsAsync(
            request.WorkspaceId,
            _requestContext.UserId,
            WorkspaceNavigationRules.PinLimit,
            ct);

        // A stored reference can outlive the caller's access to the resource (access
        // revoked, resource archived or deleted). Only references the caller can still
        // see are returned, decided by the same collection visibility the lists use.
        var userId = _requestContext.UserId;
        var visibleBoards = entries.Any(e => e.Kind == NavigationResourceKinds.Board)
            ? (await _visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, ct)).ToHashSet()
            : [];
        var visiblePages = entries.Any(e => e.Kind == NavigationResourceKinds.Page)
            ? (await _visibility.VisiblePageIdsAsync(request.WorkspaceId, userId, ct)).ToHashSet()
            : [];

        IReadOnlyList<PinnedResourceDto> result = entries
            .Where(e => e.Kind switch
            {
                NavigationResourceKinds.Board => visibleBoards.Contains(e.ResourceId),
                NavigationResourceKinds.Page => visiblePages.Contains(e.ResourceId),
                _ => false
            })
            .Select(e => new PinnedResourceDto(e.Kind, e.ResourceId, e.PinnedAt))
            .ToList();

        return Result<IReadOnlyList<PinnedResourceDto>>.Success(result);
    }
}

public class GetPinnedResourcesQueryValidator : AbstractValidator<GetPinnedResourcesQuery>
{
    public GetPinnedResourcesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
