using System.Buffers.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Infrastructure.Identity.OAuth;

/// <summary>The ID token cannot be trusted. The message names the failed check, never a token value.</summary>
public sealed class OidcIdTokenException(string reason) : Exception(reason);

/// <summary>
/// Checks the claims of an OpenID Connect ID token before any of them is believed: issuer, audience
/// (and authorized party), expiry, issue time and the nonce bound to the sign-in. Any failure throws,
/// so no account is found, linked or created from the token.
/// The signature is verified first, against the provider's published keys (see <see cref="OidcJwksCache"/>);
/// an unsigned or <c>alg=none</c> token is refused.
/// </summary>
internal static class OidcIdTokenValidator
{
    private const string TenantPlaceholder = "{tenantid}";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);

    /// <summary>The token is fresh from the exchange; one that is older was not issued for this sign-in.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);

    private static readonly string[] GoogleIssuers = ["https://accounts.google.com", "accounts.google.com"];

    /// <summary>Providers whose token endpoint returns an ID token that this sign-in must check.</summary>
    public static bool IsOidc(OAuthProvider provider) =>
        provider is OAuthProvider.Google or OAuthProvider.Microsoft;

    /// <summary>The issuers the provider's tokens may carry; a Microsoft issuer names its tenant as <c>{tenantid}</c>.</summary>
    public static IReadOnlyList<string> IssuersOf(OAuthProvider provider, OAuthProviderConfig config)
    {
        var issuers = new List<string>();
        if (provider == OAuthProvider.Google)
            issuers.AddRange(GoogleIssuers);
        if (!string.IsNullOrWhiteSpace(config.Issuer) && !issuers.Contains(config.Issuer))
            issuers.Add(config.Issuer);
        return issuers;
    }

    /// <returns>The validated claims; the caller disposes them.</returns>
    public static JsonDocument Validate(
        string idToken,
        IReadOnlyCollection<SecurityKey> signingKeys,
        IReadOnlyList<string> issuers,
        string clientId,
        string? expectedNonce,
        DateTimeOffset now)
    {
        var parts = idToken.Split('.');
        if (parts.Length != 3 || parts[1].Length == 0)
            throw new OidcIdTokenException("malformed id token");

        VerifySignature(idToken, signingKeys);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            throw new OidcIdTokenException("unreadable id token");
        }

        try
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new OidcIdTokenException("unreadable id token");

            RequireIssuer(root, issuers);
            RequireAudience(root, clientId);
            RequireTimes(root, now);
            RequireNonce(root, expectedNonce);

            if (string.IsNullOrWhiteSpace(StringClaim(root, "sub")))
                throw new OidcIdTokenException("subject missing");

            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private static readonly string[] SigningAlgorithms =
    [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512,
    ];

    /// <summary>Only the signature is checked here; the claims have their own rules below.</summary>
    private static void VerifySignature(string idToken, IReadOnlyCollection<SecurityKey> signingKeys)
    {
        var parameters = new TokenValidationParameters
        {
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidAlgorithms = SigningAlgorithms,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(idToken, parameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            throw new OidcIdTokenException("signature invalid");
        }
    }

    private static void RequireIssuer(JsonElement root, IReadOnlyList<string> issuers)
    {
        var issuer = StringClaim(root, "iss");
        if (string.IsNullOrEmpty(issuer))
            throw new OidcIdTokenException("issuer missing");

        var tenant = StringClaim(root, "tid");
        foreach (var allowed in issuers)
        {
            var expected = allowed;
            if (allowed.Contains(TenantPlaceholder, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(tenant))
                    continue;
                expected = allowed.Replace(TenantPlaceholder, tenant, StringComparison.Ordinal);
            }

            if (string.Equals(issuer, expected, StringComparison.Ordinal))
                return;
        }

        throw new OidcIdTokenException("issuer mismatch");
    }

    private static void RequireAudience(JsonElement root, string clientId)
    {
        if (!root.TryGetProperty("aud", out var aud))
            throw new OidcIdTokenException("audience missing");

        var audiences = aud.ValueKind switch
        {
            JsonValueKind.String => new List<string?> { aud.GetString() },
            JsonValueKind.Array => aud.EnumerateArray()
                .Select(a => a.ValueKind == JsonValueKind.String ? a.GetString() : null)
                .ToList(),
            _ => new List<string?>(),
        };

        if (!audiences.Contains(clientId))
            throw new OidcIdTokenException("audience mismatch");

        // Several audiences: the party the token was issued to must be us (OIDC Core 3.1.3.7 step 5).
        if (audiences.Count > 1 && !string.Equals(StringClaim(root, "azp"), clientId, StringComparison.Ordinal))
            throw new OidcIdTokenException("authorized party mismatch");
    }

    private static void RequireTimes(JsonElement root, DateTimeOffset now)
    {
        var expiresAt = SecondsClaim(root, "exp") ?? throw new OidcIdTokenException("expiry missing");
        var issuedAt = SecondsClaim(root, "iat") ?? throw new OidcIdTokenException("issue time missing");

        if (expiresAt <= now - ClockSkew)
            throw new OidcIdTokenException("id token expired");
        if (issuedAt > now + ClockSkew)
            throw new OidcIdTokenException("id token issued in the future");
        if (issuedAt < now - MaxAge - ClockSkew)
            throw new OidcIdTokenException("id token too old");
    }

    private static void RequireNonce(JsonElement root, string? expectedNonce)
    {
        var nonce = StringClaim(root, "nonce");
        if (string.IsNullOrEmpty(expectedNonce) || string.IsNullOrEmpty(nonce))
            throw new OidcIdTokenException("nonce missing");

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(nonce), Encoding.UTF8.GetBytes(expectedNonce)))
            throw new OidcIdTokenException("nonce mismatch");
    }

    public static string? StringClaim(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static DateTimeOffset? SecondsClaim(JsonElement root, string name) =>
        root.TryGetProperty(name, out var element)
        && element.ValueKind == JsonValueKind.Number
        && element.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}
