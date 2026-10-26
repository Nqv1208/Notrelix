using Notrelix.Application.Features.Identity.Profiles.Ports;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Profiles;

/// <summary>
/// Reads a person's uploaded-photo storage key through the owner-defined
/// <c>ops.co_member_avatar_storage_key</c> function, which exposes the key only for the caller and for
/// people who share a workspace membership with the caller. No other identity column is reachable here.
/// </summary>
public sealed class AvatarStorageKeyLookup : IAvatarStorageKeyReadPort
{
    private readonly ApplicationDbContext _context;

    public AvatarStorageKeyLookup(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<string?> FindAsync(Guid ownerId, CancellationToken cancellationToken) =>
        _context.Database
            .SqlQuery<string?>($"""SELECT ops.co_member_avatar_storage_key({ownerId}) AS "Value" """)
            .FirstOrDefaultAsync(cancellationToken);
}
