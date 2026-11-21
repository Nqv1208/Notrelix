using Notrelix.Application.Features.Accounts.Public.Sso;

namespace Notrelix.Application.Features.Accounts.Abstractions;

/// <summary>
/// The database's bounded single sign-on reads (owner-defined functions): the anonymous sign-in paths have no
/// Account in the request, so they never read Accounts tables directly. Infrastructure implements it over
/// PostgreSQL; hosts without database session context fall back to the Account data port.
/// </summary>
public interface IAccountSsoScopeReader
{
    Task<AccountSsoRoute?> RouteAsync(string domain, CancellationToken cancellationToken);

    /// <summary>The Account that requires SSO for a Verified domain and whether the person is its active Owner. Null when none requires it.</summary>
    Task<(Guid AccountId, bool IsOwner)?> EnforcementAsync(string domain, Guid? userId, CancellationToken cancellationToken);

    Task<bool> DomainVerifiedAsync(Guid accountId, string domain, CancellationToken cancellationToken);

    Task<AccountSsoProvider?> ProviderAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>Enters the callback scope for the Account and returns the PROTECTED client secret. Null when the Account has no Active provider.</summary>
    Task<string?> CallbackSecretAsync(Guid accountId, CancellationToken cancellationToken);

    Task<bool> JoinMemberAsync(Guid accountId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>True when ANOTHER Account holds the domain Verified (the one cross-tenant fact a claim needs).</summary>
    Task<bool> DomainClaimedByAnotherAsync(string domain, Guid accountId, CancellationToken cancellationToken);
}
