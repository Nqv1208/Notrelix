using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Infrastructure.Services;

public sealed class ActorLookupService : IActorLookupService
{
    private readonly IIdentityDbContext _context;

    public ActorLookupService(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<ActorSnapshot?> FindAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new ActorSnapshot(u.Id, u.Name, u.Avatar))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ActorSnapshot>> FindManyAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new ActorSnapshot(u.Id, u.Name, u.Avatar))
            .ToListAsync(ct);
    }
}