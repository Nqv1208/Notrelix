using Notrelix.Application.Features.Accounts.Abstractions;

namespace Notrelix.Infrastructure.Services;

/// <summary>
/// Legacy seam retained for compatibility with the account-access abstraction.
/// TenantBootstrapStore and the RLS helpers are the authoritative enforcement
/// paths; this type is intentionally not used for request authorization
/// (NRX-006, PR-IA-00 decision note).
/// </summary>
public sealed class AccountAccessEvaluator : IAccountAccessEvaluator
{
    private readonly IAccountDbContext _context;

    public AccountAccessEvaluator(IAccountDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAccountAccess(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == accountId, cancellationToken);
    }

    public Task<bool> IsAccountAdmin(Guid accountId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
