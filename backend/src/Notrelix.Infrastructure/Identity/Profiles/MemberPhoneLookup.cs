using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Profiles;

/// <summary>
/// Phones people chose to share with the members of their Workspace. On PostgreSQL the
/// read always goes through the owner-defined <c>ops.shared_member_phones</c> function, which
/// applies the same rule the Application applies (reader and subject are active non-guest members of the
/// Workspace, the subject opted in): another person's profile row is not readable by the application role.
/// Only a provider without that function (the in-memory test provider) reads the profile rows directly.
/// </summary>
public sealed class MemberPhoneLookup : IMemberPhoneLookup
{
    private readonly IIdentityDbContext _context;
    private readonly ApplicationDbContext _database;

    public MemberPhoneLookup(IIdentityDbContext context, ApplicationDbContext database)
    {
        _context = context;
        _database = database;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> FindSharedPhonesAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        if (_database.Database.IsNpgsql())
        {
            var ids = userIds.Distinct().ToArray();
            var found = await _database.Database
                .SqlQuery<PhoneRow>($"""
                    SELECT user_id, phone
                      FROM ops.shared_member_phones({workspaceId}, {ids})
                    """)
                .ToListAsync(ct);
            return found.ToDictionary(r => r.UserId, r => r.Phone);
        }

        var rows = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId) && p.PhoneVisibleToWorkspaceMembers && p.Phone != null)
            .Select(p => new { p.UserId, Phone = p.Phone! })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.UserId, r => r.Phone);
    }

    private sealed record PhoneRow(Guid UserId, string Phone);
}
