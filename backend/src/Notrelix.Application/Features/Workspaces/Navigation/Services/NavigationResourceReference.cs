namespace Notrelix.Application.Features.Workspaces.Navigation.Services;

/// <summary>
/// Validates that a navigation reference points at a real Board or Doc in the
/// request's own Account and Workspace. A resource that does not exist, or that
/// belongs to another Account or Workspace, is reported as not found so its
/// existence is not disclosed across tenants.
/// </summary>
public static class NavigationResourceReference
{
    public static async Task EnsureInWorkspaceAsync(
        IResourceLocator resourceLocator,
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
            || location.WorkspaceId != workspaceId)
        {
            throw new NotFoundException("Resource", resourceId);
        }
    }
}
