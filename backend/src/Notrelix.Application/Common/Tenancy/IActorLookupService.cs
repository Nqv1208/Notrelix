namespace Notrelix.Application.Common.Tenancy;

/// <summary>
/// Read-only lookup service for actor (user) display information.
/// Replaces direct IIdentityDbContext.Users access from other bounded contexts.
/// Implementation queries identity data; handler does not know the source.
/// </summary>
public interface IActorLookupService
{
    Task<ActorSnapshot?> FindAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<ActorSnapshot>> FindManyAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);
}

public sealed record ActorSnapshot(Guid UserId, string Name, string? AvatarUrl);