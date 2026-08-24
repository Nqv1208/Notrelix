namespace Notrelix.Infrastructure.Services;

public sealed class ResourceLocator : IResourceLocator
{
    private readonly IResourceScopeResolver _resolver;

    public ResourceLocator(IResourceScopeResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ResourceLocation?> LocateAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var location = await _resolver.ResolveAsync(resource, actorUserId, cancellationToken);
        return location is null
            ? null
            : new ResourceLocation(
                location.ResourceKind,
                location.ResourceId,
                location.AccountId,
                location.WorkspaceId);
    }
}
