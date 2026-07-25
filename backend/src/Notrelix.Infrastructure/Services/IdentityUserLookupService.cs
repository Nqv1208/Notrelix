using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Infrastructure.Services;

public sealed class IdentityUserLookupService : IIdentityUserLookupService
{
    private readonly IIdentityDbContext _context;

    public IdentityUserLookupService(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityUserSnapshot?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new IdentityUserSnapshot(
                u.Id,
                u.NormalizedEmail,
                u.EmailConfirmed,
                u.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
