namespace Notrelix.Application.Common.Context;

public interface IResourceLocator
{
    Task<ResourceLocation?> LocateAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

/// <param name="HostKind">
/// For a comment or attachment: the kind of the resource it sits on (a board item or a page), read from the same row;
/// null for every other resource.
/// </param>
public sealed record ResourceLocation(
    ResourceKind ResourceKind,
    Guid ResourceId,
    Guid AccountId,
    Guid WorkspaceId,
    ResourceKind? HostKind = null);
