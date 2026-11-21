using System.Collections.Concurrent;

namespace Notrelix.Infrastructure.Identity.OAuth;

/// <summary>
/// The provider's published signing keys. Kept for an hour; a token naming a key we do not hold
/// (the provider rotated) fetches the set again, at most once a minute so a forged <c>kid</c> cannot
/// make us hammer the provider.
/// </summary>
internal static class OidcJwksCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinRefetch = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, (JsonWebKeySet Keys, DateTimeOffset FetchedAt)> Sets = new();

    public static async Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(
        HttpClient http, string? jwksUri, string idToken, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(jwksUri))
            throw new OidcIdTokenException("signing keys not configured");

        string? kid;
        try
        {
            kid = new JwtSecurityTokenHandler().ReadJwtToken(idToken).Header.Kid;
        }
        catch (Exception ex) when (ex is ArgumentException or SecurityTokenException)
        {
            throw new OidcIdTokenException("malformed id token");
        }

        Sets.TryGetValue(jwksUri, out var cached);
        var known = cached.Keys is not null && (kid is null || cached.Keys.Keys.Any(k => k.Kid == kid));
        var stale = cached.Keys is null || now - cached.FetchedAt > Lifetime;
        var mayRefetch = cached.Keys is null || now - cached.FetchedAt > MinRefetch;

        if (stale || (!known && mayRefetch))
        {
            var json = await http.GetStringAsync(jwksUri, ct);
            cached = (new JsonWebKeySet(json), now);
            Sets[jwksUri] = cached;
        }

        return cached.Keys.GetSigningKeys().ToList();
    }
}
