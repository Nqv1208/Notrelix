namespace Notrelix.Application.Common.Security;

public interface IResourceScopeResolver
{
    Task<ResourceScopeSnapshot?> ResolveAsync(ResourceRef resource, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed record ResourceScopeSnapshot(Guid AccountId, Guid WorkspaceId, ResourceKind ResourceKind, Guid ResourceId);
