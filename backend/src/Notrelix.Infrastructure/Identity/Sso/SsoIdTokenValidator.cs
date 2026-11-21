using System.Buffers.Text;
using System.Text.Json;
using Notrelix.Infrastructure.Identity.OAuth;

namespace Notrelix.Infrastructure.Identity.Sso;

/// <summary>
/// Trusts an ID token from a company provider only when it is signed with RS256 or ES256 by a key the provider's
/// published key set holds under the token's <c>kid</c>, and its issuer, audience (and authorized party), expiry,
/// issue time, nonce and subject are right. <c>none</c>, the HMAC family and a token without a key id are refused
/// before any key is tried. The claim checks are the shared ones of <see cref="OidcIdTokenValidator"/>, here with a
/// 60 second clock skew. <c>amr</c> is never read.
/// </summary>
internal static class SsoIdTokenValidator
{
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(60);

    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256];

    /// <summary>The key id the token names, after the algorithm is found acceptable. Refuses everything else.</summary>
    public static string RequireKeyId(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length != 3 || parts[0].Length == 0)
            throw new OidcIdTokenException("malformed id token");

        JsonDocument header;
        try
        {
            header = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[0]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            throw new OidcIdTokenException("malformed id token");
        }

        using (header)
        {
            var algorithm = OidcIdTokenValidator.StringClaim(header.RootElement, "alg");
            if (algorithm is null || !AllowedAlgorithms.Contains(algorithm, StringComparer.Ordinal))
                throw new OidcIdTokenException("signature algorithm not allowed");

            var keyId = OidcIdTokenValidator.StringClaim(header.RootElement, "kid");
            return string.IsNullOrEmpty(keyId) ? throw new OidcIdTokenException("key id missing") : keyId;
        }
    }

    /// <returns>The validated claims; the caller disposes them.</returns>
    public static JsonDocument Validate(
        string idToken, JsonWebKeySet keys, string issuer, string clientId, string nonce, DateTimeOffset now)
    {
        var keyId = RequireKeyId(idToken);
        var candidates = keys.GetSigningKeys().Where(key => key.KeyId == keyId).ToList();
        if (candidates.Count == 0)
            throw new OidcIdTokenException("signing key unknown");

        return OidcIdTokenValidator.Validate(idToken, candidates, [issuer], clientId, nonce, now, clockSkew: ClockSkew);
    }
}
