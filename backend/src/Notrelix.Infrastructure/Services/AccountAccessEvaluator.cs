using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;

namespace Notrelix.Infrastructure.Services;

/// <summary>
/// Stub implementation - replace with real account membership query
/// once account bounded context is fully modeled.
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
        // TODO: Replace with real account membership query
        return await _context.Accounts
            .AnyAsync(a => a.Id == accountId, cancellationToken);
    }

    public Task<bool> IsAccountAdmin(Guid accountId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace with real admin check
        return Task.FromResult(false);
    }
}
