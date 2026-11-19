using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Features.Workspaces.Navigation.Services;

/// <summary>
/// Validates that a navigation reference points at a real Board or Doc in the
/// request's own Account and Workspace that the caller may read. A resource that
/// does not exist, that belongs to another Account or Workspace, or that the caller
/// cannot read (a Private Board nobody shared with them) is reported as not found,
/// so neither its existence nor its id is disclosed and no entry is stored for it.
/// </summary>
public static class NavigationResourceReference
{
    public static async Task EnsureInWorkspaceAsync(
        IResourceLocator resourceLocator,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext,
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        if (!ResourceKind.TryCreate(resourceKind, out var kind))
            throw new NotFoundException("Resource", resourceId);

        var location = await resourceLocator.LocateAsync(
            ResourceRef.Create(kind, resourceId),
            requestContext.UserId,
            cancellationToken);

        if (location is null
            || location.AccountId != requestContext.RequireAccountId()
            || location.WorkspaceId != workspaceId
            || !await CanReadAsync(visibility, workspaceId, requestContext.UserId, resourceKind, resourceId, cancellationToken))
        {
            throw new NotFoundException("Resource", resourceId);
        }
    }

    // The same collection visibility the lists apply; an archived Board or trashed Page is still read by the same
    // decision as an active one, so opening one keeps working.
    private static async Task<bool> CanReadAsync(
        IResourceVisibility visibility, Guid workspaceId, Guid userId, string kind, Guid resourceId, CancellationToken ct)
    {
        if (kind == NavigationResourceKinds.Board)
            return (await visibility.VisibleBoardIdsAsync(workspaceId, userId, ct)).Contains(resourceId)
                || (await visibility.VisibleArchivedBoardIdsAsync(workspaceId, userId, ct)).Contains(resourceId);
        if (kind == NavigationResourceKinds.Page)
            return (await visibility.VisiblePageIdsAsync(workspaceId, userId, ct)).Contains(resourceId)
                || (await visibility.VisibleTrashedPageIdsAsync(workspaceId, userId, ct)).Contains(resourceId);
        return false;
    }
}
