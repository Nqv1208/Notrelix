using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Navigation.Services;

namespace Notrelix.Application.Features.Workspaces.Navigation.Commands.UnpinResource;

/// <summary>
/// WX-BE-002: the current member removes their own pin. Unpinning something that
/// is not pinned is a no-op success. The resource is deliberately not required to
/// still exist: a member must be able to drop a pin whose Board or Doc was
/// deleted, otherwise stale pins would count against the cap forever. Only the
/// caller's own row in the request Workspace can be removed, so nothing leaks.
/// </summary>
public record UnpinResourceCommand(
    Guid WorkspaceId,
    string Kind,
    Guid ResourceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UnpinResourceCommandHandler : IRequestHandler<UnpinResourceCommand, Result>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly ICurrentRequestContext _requestContext;

    public UnpinResourceCommandHandler(IWorkspaceNavigationStore store, ICurrentRequestContext requestContext)
    {
        _store = store;
        _requestContext = requestContext;
    }

    public async Task<Result> Handle(UnpinResourceCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        await _store.LockMemberNavigationAsync(request.WorkspaceId, userId, ct);

        var existing = await _store.FindPinAsync(request.WorkspaceId, userId, request.Kind, request.ResourceId, ct);
        if (existing is not null)
            _store.RemovePin(existing);

        return Result.Success();
    }
}
