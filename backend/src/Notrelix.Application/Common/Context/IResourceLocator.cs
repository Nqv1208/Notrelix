namespace Notrelix.Application.Common.Context;

public interface IResourceLocator
{
    Task<ResourceLocation?> LocateAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

public sealed record ResourceLocation(
    ResourceKind ResourceKind,
    Guid ResourceId,
    Guid AccountId,
    Guid WorkspaceId);
