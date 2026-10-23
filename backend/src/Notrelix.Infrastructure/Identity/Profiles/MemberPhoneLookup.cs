using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Infrastructure.Identity.Profiles;

public sealed class MemberPhoneLookup : IMemberPhoneLookup
{
    private readonly IIdentityDbContext _context;

    public MemberPhoneLookup(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> FindSharedPhonesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId) && p.PhoneVisibleToWorkspaceMembers && p.Phone != null)
            .Select(p => new { p.UserId, Phone = p.Phone! })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.UserId, r => r.Phone);
    }
}
