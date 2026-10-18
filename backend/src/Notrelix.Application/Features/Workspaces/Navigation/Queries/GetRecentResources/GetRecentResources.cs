using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
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

    public GetRecentResourcesQueryHandler(IWorkspaceNavigationStore store, ICurrentRequestContext requestContext)
    {
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

        IReadOnlyList<RecentResourceDto> result = entries
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
