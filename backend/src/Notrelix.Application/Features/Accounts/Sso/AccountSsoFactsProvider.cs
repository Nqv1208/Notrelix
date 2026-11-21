using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Sso;

/// <summary>
/// Producer implementation of <see cref="IAccountSsoFacts"/>. Where the host sets database session context the anonymous
/// sign-in paths read through the database's bounded SSO functions (<see cref="IAccountSsoScopeReader"/>); elsewhere
/// (unconstrained connections, in-memory hosts) the same rules run over the Account data port.
/// </summary>
public sealed class AccountSsoFactsProvider : IAccountSsoFacts
{
    private readonly IAccountDbContext _context;
    private readonly ISecretEncryptor _encryptor;
    private readonly IAccountGrantProjectionService _grants;
    private readonly IAccountSsoScopeReader? _scope;
    private readonly IRlsSessionContext? _rls;

    public AccountSsoFactsProvider(
        IAccountDbContext context,
        ISecretEncryptor encryptor,
        IAccountGrantProjectionService grants,
        IAccountSsoScopeReader? scope = null,
        IRlsSessionContext? rls = null)
    {
        _context = context;
        _encryptor = encryptor;
        _grants = grants;
        _scope = scope;
        _rls = rls;
    }

    private bool Bounded => _scope is not null && _rls?.IsSessionContextEnabled == true;

    public async Task<AccountSsoRoute?> FindRouteAsync(string email, CancellationToken cancellationToken)
    {
        var domain = AccountSsoSupport.DomainOfEmail(email);
        if (domain is null) return null;
        if (Bounded) return await _scope!.RouteAsync(domain, cancellationToken);

        var domainRow = await VerifiedDomains(domain).FirstOrDefaultAsync(cancellationToken);
        if (domainRow is null) return null;
        var account = await OpenAccounts().FirstOrDefaultAsync(a => a.Id == domainRow.AccountId, cancellationToken);
        var provider = await ActiveProviders().FirstOrDefaultAsync(p => p.AccountId == domainRow.AccountId, cancellationToken);
        if (account is null || provider is null) return null;
        return new AccountSsoRoute(account.Id, account.Name, provider.Name, await RequiresSsoAsync(account.Id, cancellationToken));
    }

    public async Task<AccountSsoProvider?> GetProviderAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (Bounded) return await _scope!.ProviderAsync(accountId, cancellationToken);

        if (!await OpenAccounts().AnyAsync(a => a.Id == accountId, cancellationToken)) return null;
        var provider = await ActiveProviders().FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);
        return provider?.ClientId is null
            ? null
            : new AccountSsoProvider(provider.Name, provider.Issuer, provider.ClientId, provider.JitProvisioningEnabled);
    }

    public async Task<AccountSsoCallbackProvider?> GetCallbackProviderAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var provider = await GetProviderAsync(accountId, cancellationToken);
        if (provider is null) return null;

        var protectedSecret = Bounded
            ? await _scope!.CallbackSecretAsync(accountId, cancellationToken)
            : (await ActiveProviders().FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken))?.ClientSecretProtected;
        if (string.IsNullOrEmpty(protectedSecret)) return null;

        return new AccountSsoCallbackProvider(
            provider.Name, provider.Issuer, provider.ClientId,
            _encryptor.Unprotect(protectedSecret, AccountSsoSupport.ClientSecretPurpose), provider.JitProvisioning);
    }

    public async Task<bool> IsEmailDomainVerifiedAsync(Guid accountId, string email, CancellationToken cancellationToken)
    {
        var domain = AccountSsoSupport.DomainOfEmail(email);
        if (domain is null) return false;
        if (Bounded) return await _scope!.DomainVerifiedAsync(accountId, domain, cancellationToken);
        return await VerifiedDomains(domain).AnyAsync(d => d.AccountId == accountId, cancellationToken);
    }

    public async Task<AccountSsoEnforcement> CheckEnforcementAsync(
        string email, Guid? userId, AccountSsoSignInMethod method, CancellationToken cancellationToken)
    {
        var domain = AccountSsoSupport.DomainOfEmail(email);
        if (domain is null) return AccountSsoEnforcement.NotRequired;

        (Guid AccountId, bool IsOwner)? enforcing;
        if (Bounded)
        {
            enforcing = await _scope!.EnforcementAsync(domain, userId, cancellationToken);
        }
        else
        {
            enforcing = null;
            var accountId = await VerifiedDomains(domain).Select(d => (Guid?)d.AccountId).FirstOrDefaultAsync(cancellationToken);
            if (accountId is { } id
                && await _context.Accounts.AnyAsync(a => a.Id == id && a.DeletedAt == null && a.Status != AccountStatus.Closed, cancellationToken)
                && await RequiresSsoAsync(id, cancellationToken))
            {
                var isOwner = userId is { } user && await _context.AccountMembers.AnyAsync(
                    m => m.AccountId == id && m.UserId == user && m.Role == AccountRole.Owner
                        && m.Status == AccountMemberStatus.Active && m.DeletedAt == null, cancellationToken);
                enforcing = (id, isOwner);
            }
        }

        if (enforcing is not { } found) return AccountSsoEnforcement.NotRequired;
        // The Owner keeps the password as the break-glass; a social sign-in is exempt for nobody.
        if (method == AccountSsoSignInMethod.Password && found.IsOwner) return AccountSsoEnforcement.NotRequired;
        return new AccountSsoEnforcement(true, found.AccountId);
    }

    public async Task<bool> JoinAsMemberAsync(
        Guid accountId, Guid userId, string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!await IsEmailDomainVerifiedAsync(accountId, email, cancellationToken)) return false;
        if (Bounded) return await _scope!.JoinMemberAsync(accountId, userId, now, cancellationToken);

        if (!await OpenAccounts().AnyAsync(a => a.Id == accountId, cancellationToken)
            || !await ActiveProviders().AnyAsync(p => p.AccountId == accountId, cancellationToken))
            return false;

        var member = _context.AccountMembers.Local.FirstOrDefault(m => m.AccountId == accountId && m.UserId == userId)
            ?? await _context.AccountMembers.FirstOrDefaultAsync(m => m.AccountId == accountId && m.UserId == userId, cancellationToken);
        if (member is not null) return member.Status == AccountMemberStatus.Active;

        _context.AccountMembers.Add(AccountMember.Create(accountId, userId, AccountRole.Member, userId, now));
        await _grants.SyncAccountMemberGrantAsync(accountId, userId, AccountRole.Member, now, cancellationToken);
        return true;
    }

    private IQueryable<AccountDomain> VerifiedDomains(string domain) =>
        _context.AccountDomains.AsNoTracking()
            .Where(d => d.Domain == domain && d.VerificationStatus == DomainVerificationStatus.Verified);

    private IQueryable<Account> OpenAccounts() =>
        _context.Accounts.AsNoTracking()
            .Where(a => a.DeletedAt == null && (a.Status == AccountStatus.Active || a.Status == AccountStatus.Trialing));

    private IQueryable<AccountIdentityProvider> ActiveProviders() =>
        _context.AccountIdentityProviders.AsNoTracking()
            .Where(p => p.ProviderType == "Oidc" && p.Status == AccountIdentityProvider.StatusActive);

    private Task<bool> RequiresSsoAsync(Guid accountId, CancellationToken cancellationToken) =>
        _context.AccountSettingsEntities.AsNoTracking().AnyAsync(
            s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso
                && s.SettingValue == AccountSecuritySettingKeys.True, cancellationToken);
}
