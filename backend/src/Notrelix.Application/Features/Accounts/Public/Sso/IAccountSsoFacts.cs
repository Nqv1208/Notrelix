namespace Notrelix.Application.Features.Accounts.Public.Sso;

/// <summary>
/// Producer-owned public query and action surface of enterprise single sign-on for the Identity sign-in flow.
/// Consumers never read Accounts tables. Every answer that depends on an email's domain is a function of the
/// domain only: the same answer for a known and an unknown address, and the same <c>null</c> for an unverified
/// domain, an Account with no Active provider, a closed Account and no domain at all.
/// </summary>
public interface IAccountSsoFacts
{
    /// <summary>
    /// Who the email's domain signs in with. Null unless the domain is Verified for an open Account that has an Active
    /// provider (null is indistinguishable for every reason).
    /// </summary>
    Task<AccountSsoRoute?> FindRouteAsync(string email, CancellationToken cancellationToken);

    /// <summary>The Active provider's configuration without the client secret (for building the authorization address). Null when none.</summary>
    Task<AccountSsoProvider?> GetProviderAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// The Active provider's configuration including the DECRYPTED client secret, for the callback only. It enters the
    /// database's SSO callback scope, which replaces any earlier scope of the transaction: read it before entering the
    /// credential or registration scope. Null when the Account has no Active provider.
    /// </summary>
    Task<AccountSsoCallbackProvider?> GetCallbackProviderAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>True when the email's domain is a Verified domain of exactly this Account.</summary>
    Task<bool> IsEmailDomainVerifiedAsync(Guid accountId, string email, CancellationToken cancellationToken);

    /// <summary>
    /// Whether signing in with <paramref name="method"/> as this email is refused because the email's Verified domain
    /// belongs to an Account that requires SSO. A password sign-in by an active Owner of that Account is exempt
    /// (break-glass); a social sign-in is exempt for nobody. <paramref name="userId"/> is only used for the Owner check.
    /// </summary>
    Task<AccountSsoEnforcement> CheckEnforcementAsync(string email, Guid? userId, AccountSsoSignInMethod method, CancellationToken cancellationToken);

    /// <summary>
    /// Joins the person to the Account as a Member (account member role Member, never a workspace role, no personal
    /// Account). Only when the email is on a Verified domain of that Account whose provider is Active. True when the
    /// person is an active Member afterwards (newly joined or already); false when refused or the existing
    /// membership is not active (it is never reactivated).
    /// </summary>
    Task<bool> JoinAsMemberAsync(Guid accountId, Guid userId, string email, DateTimeOffset now, CancellationToken cancellationToken);
}

/// <summary>How a person is signing in, for the enforcement check.</summary>
public enum AccountSsoSignInMethod
{
    Password,
    Social,
}

/// <summary>A routable domain: the Account, its display name, the provider's display name and whether SSO is required.</summary>
public sealed record AccountSsoRoute(Guid AccountId, string AccountName, string ProviderName, bool Enforced);

/// <summary>The Active provider's public configuration.</summary>
public sealed record AccountSsoProvider(string Name, string Issuer, string ClientId, bool JitProvisioning);

/// <summary>The Active provider's configuration with the decrypted client secret. Never log or persist <see cref="ClientSecret"/>.</summary>
public sealed record AccountSsoCallbackProvider(string Name, string Issuer, string ClientId, string ClientSecret, bool JitProvisioning)
{
    public override string ToString() => $"AccountSsoCallbackProvider {{ Name = {Name}, Issuer = {Issuer}, ClientId = {ClientId} }}";
}

/// <summary>The enforcement answer: whether sign-in is refused and, when it is, which Account requires SSO.</summary>
public sealed record AccountSsoEnforcement(bool Required, Guid? AccountId)
{
    public static AccountSsoEnforcement NotRequired { get; } = new(false, null);
}
