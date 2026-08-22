namespace Notrelix.Application.Common.Security;

public sealed class ResourceAuthorizationSnapshotStore : IResourceAuthorizationSnapshotStore
{
    private readonly IReadOnlyDictionary<ResourceKind, IResourceAuthorizationSnapshotResolver> _resolvers;

    public ResourceAuthorizationSnapshotStore(IEnumerable<IResourceAuthorizationSnapshotResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(x => x.ResourceKind);
    }

    public Task<ResourceAuthorizationSnapshot?> ResolveAsync(
        ResourceKind resourceKind,
        Guid resourceId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        return _resolvers.TryGetValue(resourceKind, out var resolver)
            ? resolver.ResolveAsync(resourceId, actorId, cancellationToken)
            : Task.FromResult<ResourceAuthorizationSnapshot?>(null);
    }
}
