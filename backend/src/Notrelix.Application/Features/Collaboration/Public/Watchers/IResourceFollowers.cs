namespace Notrelix.Application.Features.Collaboration.Public.Watchers;

/// <summary>
/// Collaboration-owned read of who follows a resource. A resource is addressed by its full (kind, id)
/// identity. Consumers compose this boundary into their own read models and notification fan-out; they
/// never query Collaboration persistence directly.
/// </summary>
public interface IResourceFollowers
{
    /// <summary>The people who follow the resource, in no particular order.</summary>
    Task<IReadOnlyList<Guid>> FollowerIdsAsync(string kind, Guid resourceId, CancellationToken cancellationToken);

    /// <summary>
    /// The resources of one kind in one Workspace the person follows, newest follow first, at most
    /// <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> FollowedResourceIdsAsync(
        string kind, Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken);
}
