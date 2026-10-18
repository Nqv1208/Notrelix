using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
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

    public GetPinnedResourcesQueryHandler(IWorkspaceNavigationStore store, ICurrentRequestContext requestContext)
    {
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

        IReadOnlyList<PinnedResourceDto> result = entries
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
