using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Domains;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Members;

/// <summary>
/// Producer-owned implementation of the Accounts verified-domain public surface.
/// "Primary" is the earliest verified domain: the Account model has no primary flag, so the first
/// domain the company proved it owns is the stable choice (documented in the Accounts product doc).
/// </summary>
public sealed class AccountDomainFactsProvider : IAccountDomainFacts
{
    private readonly IAccountDbContext _context;

    public AccountDomainFactsProvider(IAccountDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetPrimaryVerifiedDomainAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await _context.AccountDomains
            .AsNoTracking()
            .Where(d => d.AccountId == accountId && d.VerificationStatus == DomainVerificationStatus.Verified)
            .OrderBy(d => d.VerifiedAt)
            .ThenBy(d => d.Domain)
            .Select(d => d.Domain)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
