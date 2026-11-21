namespace Notrelix.Application.Features.Identity.Sso;

/// <summary>
/// What the callback must remember about one started company sign-in. Held server-side only, single use.
/// <see cref="BindingHash"/> is the SHA-256 of the random value only the starting browser holds (in a cookie); the value itself is never stored.
/// </summary>
public sealed record SsoLoginState(
    string State,
    Guid AccountId,
    string Email,
    string Nonce,
    string CodeVerifier,
    string RedirectUri,
    string? ReturnUrl,
    string Issuer,
    string BindingHash,
    DateTimeOffset ExpiresAt);

public static class SsoBinding
{
    public static string Hash(string binding) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(binding)));

    /// <summary>Constant-time check of a presented binding against the stored hash; a missing binding never matches.</summary>
    public static bool Matches(string? presented, string storedHash) =>
        !string.IsNullOrEmpty(presented)
        && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(Hash(presented)), System.Text.Encoding.ASCII.GetBytes(storedHash));
}

public interface ISsoStateStore
{
    Task StoreAsync(SsoLoginState state, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>One atomic read-and-remove: a state works once, however many callbacks race with it.</summary>
    Task<SsoLoginState?> ConsumeAsync(string state, CancellationToken cancellationToken);
}

/// <summary>Everything the token endpoint and the ID token check need. <see cref="ClientSecret"/> is never logged or returned.</summary>
public sealed record SsoCodeRedemption(
    string Issuer,
    string ClientId,
    string ClientSecret,
    string Code,
    string CodeVerifier,
    string RedirectUri,
    string Nonce)
{
    public override string ToString() => $"SsoCodeRedemption {{ Issuer = {Issuer}, ClientId = {ClientId} }}";
}

/// <summary>The person the provider vouched for: only what the ID token's claims state, after every check passed.</summary>
public sealed record SsoIdentityClaims(string Subject, string? Email, bool EmailVerified, string? Name);

/// <summary>The code could not be exchanged or the ID token cannot be trusted. The message names the failed check, never a value.</summary>
public sealed class SsoRedemptionException(string reason) : Exception(reason);

/// <summary>
/// Exchanges the authorization code at the provider's token endpoint (read from its discovery document) and
/// validates the returned ID token: signature against the provider's keys, issuer, audience, expiry and nonce.
/// Any failure throws <see cref="SsoRedemptionException"/>.
/// </summary>
public interface ISsoCodeRedeemer
{
    Task<SsoIdentityClaims> RedeemAsync(SsoCodeRedemption redemption, CancellationToken cancellationToken);
}
