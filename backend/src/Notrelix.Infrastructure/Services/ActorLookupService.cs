using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Services;

/// <summary>
/// Resolves actor display facts (name, avatar) for ids the Application already decided to show. Reads go
/// through the owner-defined <c>ops.co_member_profiles</c> function, which returns only the display name and
/// avatar of people who share a workspace membership with the caller (in any membership state; a person
/// always resolves themselves). A user the caller has no relationship to is simply not returned, exactly
/// like an unknown id. No other identity column is reachable through this path.
/// </summary>
public sealed class ActorLookupService : IActorLookupService
{
    private readonly ApplicationDbContext _context;

    public ActorLookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActorSnapshot?> FindAsync(Guid userId, CancellationToken ct)
    {
        var rows = await FindManyAsync([userId], ct);
        return rows.Count == 0 ? null : rows[0];
    }

    public async Task<IReadOnlyList<ActorSnapshot>> FindManyAsync(IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
            return [];

        var ids = userIds.Distinct().ToArray();
        var rows = await _context.Database
            .SqlQuery<CoMemberProfileRow>($"""
                SELECT user_id, name, avatar
                  FROM ops.co_member_profiles({ids})
                """)
            .ToListAsync(ct);

        return rows.Select(r => new ActorSnapshot(r.UserId, r.Name, r.Avatar)).ToList();
    }

    private sealed record CoMemberProfileRow(Guid UserId, string Name, string? Avatar);
}
