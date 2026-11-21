using Notrelix.Application.Features.Accounts.Public.Sso;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>Single sign-on facts that answer from settable values (nothing routes and nothing is enforced until a test says so) and record what was asked.</summary>
public sealed class FakeAccountSsoFacts : IAccountSsoFacts
{
    public AccountSsoRoute? Route { get; set; }
    public AccountSsoProvider? Provider { get; set; }
    public AccountSsoCallbackProvider? CallbackProvider { get; set; }
    public bool DomainVerified { get; set; } = true;
    public bool JoinResult { get; set; } = true;
    public AccountSsoEnforcement Enforcement { get; set; } = AccountSsoEnforcement.NotRequired;

    public List<(string Email, Guid? UserId, AccountSsoSignInMethod Method)> EnforcementChecks { get; } = [];
    public List<(Guid AccountId, Guid UserId)> Joins { get; } = [];
    public int CallbackProviderReads { get; private set; }

    public Task<AccountSsoRoute?> FindRouteAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Route);

    public Task<AccountSsoProvider?> GetProviderAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult(Provider);

    public Task<AccountSsoCallbackProvider?> GetCallbackProviderAsync(Guid accountId, CancellationToken cancellationToken)
    {
        CallbackProviderReads++;
        return Task.FromResult(CallbackProvider);
    }

    public Task<bool> IsEmailDomainVerifiedAsync(Guid accountId, string email, CancellationToken cancellationToken) => Task.FromResult(DomainVerified);

    public Task<AccountSsoEnforcement> CheckEnforcementAsync(
        string email, Guid? userId, AccountSsoSignInMethod method, CancellationToken cancellationToken)
    {
        EnforcementChecks.Add((email, userId, method));
        return Task.FromResult(Enforcement);
    }

    public Task<bool> JoinAsMemberAsync(Guid accountId, Guid userId, string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Joins.Add((accountId, userId));
        return Task.FromResult(JoinResult);
    }
}
