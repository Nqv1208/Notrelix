using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Application.Features.Workspaces.Public.Lifecycle;

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
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IPersonalWorkspaceRequest
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class PinResourceCommandHandler : IRequestHandler<PinResourceCommand, Result>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly IResourceLocator _resourceLocator;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PinResourceCommandHandler(
        IWorkspaceNavigationStore store,
        IResourceLocator resourceLocator,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _store = store;
        _resourceLocator = resourceLocator;
        _visibility = visibility;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PinResourceCommand request, CancellationToken ct)
    {
        await NavigationResourceReference.EnsureInWorkspaceAsync(
            _resourceLocator, _visibility, _requestContext, request.WorkspaceId, request.Kind, request.ResourceId, ct);

        var userId = _requestContext.UserId;

        await _store.LockMemberNavigationAsync(request.WorkspaceId, userId, ct);

        var existing = await _store.FindPinAsync(request.WorkspaceId, userId, request.Kind, request.ResourceId, ct);
        if (existing is not null)
            return Result.Success();

        // The cap counts the pins the member can still read. Pins to something gone or no longer shared are
        // deleted here, in the same transaction as the new pin, so a member never stays stuck below the cap
        // with entries the list hides. If the cap refuses the pin, the request rolls the deletes back too.
        var stored = await _store.LoadPinsAsync(request.WorkspaceId, userId, ct);
        var readable = await NavigationResourceReference.ReadableAsync(
            _visibility, request.WorkspaceId, userId, stored.Select(p => (p.ResourceKind, p.ResourceId)).ToList(), ct);
        var unreadable = stored.Where(p => !readable.Contains((p.ResourceKind, p.ResourceId))).ToList();
        foreach (var stale in unreadable)
            _store.RemovePin(stale);

        var pin = PinnedResource.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            userId,
            request.Kind,
            request.ResourceId,
            stored.Count - unreadable.Count,
            _dateTimeProvider.UtcNow);

        _store.AddPin(pin);
        return Result.Success();
    }
}
