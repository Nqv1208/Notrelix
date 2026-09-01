using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.Facts;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Features.Identity.Users.Services;

/// <summary>
/// Producer-owned implementation of the Identity public user facts surface.
/// Maps Identity lifecycle state into stable participation semantics inside
/// Application, where that business policy belongs.
/// </summary>
public sealed class IdentityUserFactsProvider : IIdentityUserFacts
{
    private readonly IIdentityDbContext _context;

    public IdentityUserFactsProvider(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityUserFact?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.NormalizedEmail,
                u.EmailConfirmed,
                u.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
            return null;

        var canParticipate = snapshot.Status is UserStatus.Active or UserStatus.PendingVerification;

        return new IdentityUserFact(
            snapshot.Id,
            snapshot.NormalizedEmail,
            snapshot.EmailConfirmed,
            canParticipate);
    }
}
