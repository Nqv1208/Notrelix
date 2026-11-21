using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;

namespace Notrelix.Infrastructure.Accounts.Sso;

/// <summary>
/// The anonymous sign-in paths' view of single sign-on: only through the owner-defined <c>ops.sso_*</c> functions of
/// 035_sso_scope.sql, each keyed by the verified domain or an account id the server derived itself. No Accounts table is
/// read with a broad policy. The client secret comes only from the callback scope.
/// </summary>
public sealed class AccountSsoScopeReader : IAccountSsoScopeReader
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountSsoScopeReader> _logger;

    public AccountSsoScopeReader(ApplicationDbContext context, ILogger<AccountSsoScopeReader> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountSsoRoute?> RouteAsync(string domain, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<RouteRow>($"SELECT account_id, account_name, provider_name, enforced FROM ops.sso_route({domain})")
            .ToListAsync(cancellationToken);
        return rows.Count == 1 ? new AccountSsoRoute(rows[0].AccountId, rows[0].AccountName, rows[0].ProviderName, rows[0].Enforced) : null;
    }

    public async Task<(Guid AccountId, bool IsOwner)?> EnforcementAsync(string domain, Guid? userId, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<EnforcementRow>($"SELECT account_id, is_owner FROM ops.sso_enforcement({domain}, {userId}::uuid)")
            .ToListAsync(cancellationToken);
        return rows.Count == 1 ? (rows[0].AccountId, rows[0].IsOwner) : null;
    }

    public Task<bool> DomainVerifiedAsync(Guid accountId, string domain, CancellationToken cancellationToken) =>
        ScalarAsync($"SELECT ops.sso_domain_verified({accountId}, {domain}) AS \"Value\"", cancellationToken);

    public async Task<AccountSsoProvider?> ProviderAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<ProviderRow>($"SELECT name, issuer, client_id, jit_provisioning FROM ops.sso_provider_public({accountId})")
            .ToListAsync(cancellationToken);
        return rows.Count == 1 ? new AccountSsoProvider(rows[0].Name, rows[0].Issuer, rows[0].ClientId, rows[0].JitProvisioning) : null;
    }

    public async Task<string?> CallbackSecretAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("The SSO callback scope is transaction-local and must be entered inside an open transaction.");

        var entered = await EnterCallbackScopeAsync(accountId, cancellationToken);
        if (!entered) return null;

        var rows = await _context.Database
            .SqlQuery<string?>($"SELECT ops.sso_callback_secret() AS \"Value\"")
            .ToListAsync(cancellationToken);
        return rows.Count == 1 ? rows[0] : null;
    }

    /// <summary>Enters the database callback scope for an Account the server derived itself. Nothing secret exists yet here.</summary>
    private async Task<bool> EnterCallbackScopeAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var entered = await ScalarAsync($"SELECT ops.enter_sso_callback_scope({accountId}) AS \"Value\"", cancellationToken);
        RlsScopeMetrics.Record("sso-callback", "SsoCallback", entered);
        _logger.LogInformation(
            entered ? "SSO callback scope entered | account {AccountId}" : "SSO callback scope not entered | account {AccountId}", accountId);
        return entered;
    }

    public Task<bool> JoinMemberAsync(Guid accountId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var at = now.ToUniversalTime();
        return ScalarAsync($"SELECT ops.sso_join_member({accountId}, {userId}, {at}) AS \"Value\"", cancellationToken);
    }

    public Task<bool> DomainClaimedByAnotherAsync(string domain, Guid accountId, CancellationToken cancellationToken) =>
        ScalarAsync($"SELECT ops.sso_domain_claimed_by_other({domain}, {accountId}) AS \"Value\"", cancellationToken);

    private async Task<bool> ScalarAsync(FormattableString sql, CancellationToken cancellationToken)
    {
        var rows = await _context.Database.SqlQuery<bool>(sql).ToListAsync(cancellationToken);
        return rows.Count == 1 && rows[0];
    }

    private sealed record RouteRow(Guid AccountId, string AccountName, string ProviderName, bool Enforced);

    private sealed record EnforcementRow(Guid AccountId, bool IsOwner);

    private sealed record ProviderRow(string Name, string Issuer, string ClientId, bool JitProvisioning);
}
