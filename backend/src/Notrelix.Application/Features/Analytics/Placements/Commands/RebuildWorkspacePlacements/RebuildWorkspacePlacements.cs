using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Placements.Services;

namespace Notrelix.Application.Features.Analytics.Placements.Commands.RebuildWorkspacePlacements;

/// <summary>
/// Analytics-owned rebuild use case: fetches the producer-owned placement
/// snapshot through the Analytics source/rebuild Port (delegated by
/// Infrastructure to the WorkManagement Public projection-source contract)
/// and reconciles the local projection for one Workspace. Not an outbox
/// replay; no Work DbContext access.
/// </summary>
public record RebuildWorkspacePlacementsCommand(Guid WorkspaceId)
    : ICommand<Result<int>>, IAuthenticatedRequest, IWorkspaceRequest, IWriteRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource =>
        ResourceRef.Create(ResourceKind.Create("analytics.placement"), WorkspaceId, WorkspaceId);
}

public class RebuildWorkspacePlacementsCommandHandler
    : IRequestHandler<RebuildWorkspacePlacementsCommand, Result<int>>
{
    private readonly IWorkItemProjectionSourceAdapter _projectionSource;
    private readonly WorkspaceWorkItemPlacementService _service;

    public RebuildWorkspacePlacementsCommandHandler(
        IWorkItemProjectionSourceAdapter projectionSource,
        WorkspaceWorkItemPlacementService service)
    {
        _projectionSource = projectionSource;
        _service = service;
    }

    public async Task<Result<int>> Handle(
        RebuildWorkspacePlacementsCommand request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _projectionSource.GetWorkspacePlacementsAsync(
            request.WorkspaceId, cancellationToken);

        await _service.RebuildWorkspaceAsync(request.WorkspaceId, snapshot, cancellationToken);

        return Result<int>.Success(snapshot.Count);
    }
}
