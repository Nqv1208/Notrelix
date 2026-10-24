using Notrelix.Application.Features.Identity.Users.Ports;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Users;

/// <summary>
/// Reads other people's sign-in email for one Workspace. On PostgreSQL the
/// read always goes through the owner-defined <c>ops.disclosable_member_emails</c> function, which applies the same
/// rule the Workspaces disclosure applies (reader is an active Owner or Admin of the Workspace, subjects are
/// active non-guest members of it): another person's user row is not readable by the application role. Only a
/// provider without that function (the in-memory test provider) reads the user rows directly.
/// </summary>
public sealed class MemberEmailReader : IMemberEmailReadPort
{
    private readonly ApplicationDbContext _context;

    public MemberEmailReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> FindAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        if (_context.Database.IsNpgsql())
        {
            var ids = userIds.Distinct().ToArray();
            var found = await _context.Database
                .SqlQuery<EmailRow>($"""
                    SELECT user_id, email
                      FROM ops.disclosable_member_emails({workspaceId}, {ids})
                    """)
                .ToListAsync(cancellationToken);
            return found.ToDictionary(r => r.UserId, r => r.Email);
        }

        var rows = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.NormalizedEmail })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Id, r => r.NormalizedEmail);
    }

    private sealed record EmailRow(Guid UserId, string Email);
}
