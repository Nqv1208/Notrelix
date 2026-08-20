namespace Notrelix.Application.Common.Security;

public enum ResourceAudience
{
    Restricted,
    Workspace
}

public enum ResourceMemberAccess
{
    Viewer,
    Editor,
    Manager
}

public sealed record ResourceAuthorizationSnapshot(
    Guid WorkspaceId,
    ResourceAudience Audience,
    ResourceMemberAccess? MemberAccess);

public interface IResourceAuthorizationSnapshotResolver
{
    ResourceKind ResourceKind { get; }

    Task<ResourceAuthorizationSnapshot?> ResolveAsync(
        Guid resourceId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public interface IResourceAuthorizationSnapshotStore
{
    Task<ResourceAuthorizationSnapshot?> ResolveAsync(
        ResourceKind resourceKind,
        Guid resourceId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}
