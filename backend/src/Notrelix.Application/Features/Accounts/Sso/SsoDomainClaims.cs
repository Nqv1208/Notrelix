using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Sso;

/// <summary>
/// Whether another Account already holds a domain Verified. Under database session context an Account cannot see
/// another Account's rows, so the one cross-tenant fact comes from the bounded database function; elsewhere from the data
/// port. The database's partial unique index on Verified domains is the last line either way.
/// </summary>
public sealed class SsoDomainClaims
{
    private readonly IAccountDbContext _context;
    private readonly IAccountSsoScopeReader? _scope;
    private readonly IRlsSessionContext? _rls;

    public SsoDomainClaims(IAccountDbContext context, IAccountSsoScopeReader? scope = null, IRlsSessionContext? rls = null)
    {
        _context = context;
        _scope = scope;
        _rls = rls;
    }

    public Task<bool> IsClaimedByAnotherAsync(string domain, Guid accountId, CancellationToken cancellationToken) =>
        _scope is not null && _rls?.IsSessionContextEnabled == true
            ? _scope.DomainClaimedByAnotherAsync(domain, accountId, cancellationToken)
            : _context.AccountDomains.AsNoTracking().AnyAsync(
                d => d.Domain == domain && d.AccountId != accountId && d.VerificationStatus == DomainVerificationStatus.Verified,
                cancellationToken);

    /// <summary>
    /// Stores the verification now. Two Accounts proving the same domain at the same moment both pass the claimed check;
    /// the partial unique index on Verified rows lets one win and the loser gets the same refusal as a domain that was
    /// already claimed.
    /// </summary>
    public async Task SaveVerifiedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).Contains(VerifiedDomainIndex, StringComparison.Ordinal))
        {
            throw new ConflictException("Another account has already verified this domain.", AccountSsoErrorCodes.DomainClaimed);
        }
    }

    private const string VerifiedDomainIndex = "idx_account_domains_domain_verified";
}
