using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;

/// <summary>
/// WX-BE-002: the current member pins a Board or Doc. Pinning an already pinned
/// resource is a semantic no-op (the original pin time is kept), so the PUT is
/// naturally idempotent. A new pin over the per-member cap is rejected with
/// <c>Workspaces_Navigation_PinLimitReached</c>.
/// </summary>
public record PinResourceCommand(
    Guid WorkspaceId,
    string Kind,
    Guid ResourceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class PinResourceCommandHandler : IRequestHandler<PinResourceCommand, Result>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly IResourceLocator _resourceLocator;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PinResourceCommandHandler(
        IWorkspaceNavigationStore store,
        IResourceLocator resourceLocator,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _store = store;
        _resourceLocator = resourceLocator;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PinResourceCommand request, CancellationToken ct)
    {
        await NavigationResourceReference.EnsureInWorkspaceAsync(
            _resourceLocator, _requestContext, request.WorkspaceId, request.Kind, request.ResourceId, ct);

        var userId = _requestContext.UserId;

        await _store.LockMemberNavigationAsync(request.WorkspaceId, userId, ct);

        var existing = await _store.FindPinAsync(request.WorkspaceId, userId, request.Kind, request.ResourceId, ct);
        if (existing is not null)
            return Result.Success();

        var pinCount = await _store.CountPinsAsync(request.WorkspaceId, userId, ct);
        var pin = PinnedResource.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            userId,
            request.Kind,
            request.ResourceId,
            pinCount,
            _dateTimeProvider.UtcNow);

        _store.AddPin(pin);
        return Result.Success();
    }
}
