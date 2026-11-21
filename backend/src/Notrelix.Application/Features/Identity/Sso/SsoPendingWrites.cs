using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Sso;

/// <summary>
/// Stores what the company sign-in has staged so far (a new person, a new identity) before the request ends. The Account
/// side joins a person only after reading the stored, email-confirmed user, and the Account's MFA rules read the stored
/// membership, so the callback stores at those two points; everything else is saved when the pipeline saves.
/// </summary>
public sealed class SsoPendingWrites
{
    private readonly IIdentityDbContext _context;

    public SsoPendingWrites(IIdentityDbContext context) => _context = context;

    public Task StoreNowAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
