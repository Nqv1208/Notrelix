using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;

/// <summary>
/// WX-BE-001: the current member's recently opened Boards and Docs in a
/// Workspace, newest first. References only; the client joins display names
/// from the Board and Doc lists it is authorized to see (WX-DEC-011).
/// </summary>
public record GetRecentResourcesQuery(
    Guid WorkspaceId
) : IQuery<Result<IReadOnlyList<RecentResourceDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public record RecentResourceDto(string Kind, Guid ResourceId, DateTimeOffset LastOpenedAt);

public class GetRecentResourcesQueryHandler : IRequestHandler<GetRecentResourcesQuery, Result<IReadOnlyList<RecentResourceDto>>>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IResourceVisibility _visibility;

    public GetRecentResourcesQueryHandler(
        IWorkspaceNavigationStore store,
        ICurrentRequestContext requestContext,
        IResourceVisibility visibility)
    {
        _visibility = visibility;
        _store = store;
        _requestContext = requestContext;
    }

    public async Task<Result<IReadOnlyList<RecentResourceDto>>> Handle(GetRecentResourcesQuery request, CancellationToken ct)
    {
        var entries = await _store.ListRecentsAsync(
            request.WorkspaceId,
            _requestContext.UserId,
            WorkspaceNavigationRules.RecentRetentionLimit,
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

        IReadOnlyList<RecentResourceDto> result = entries
            .Where(e => e.Kind switch
            {
                NavigationResourceKinds.Board => visibleBoards.Contains(e.ResourceId),
                NavigationResourceKinds.Page => visiblePages.Contains(e.ResourceId),
                _ => false
            })
            .Select(e => new RecentResourceDto(e.Kind, e.ResourceId, e.LastOpenedAt))
            .ToList();

        return Result<IReadOnlyList<RecentResourceDto>>.Success(result);
    }
}

public class GetRecentResourcesQueryValidator : AbstractValidator<GetRecentResourcesQuery>
{
    public GetRecentResourcesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
