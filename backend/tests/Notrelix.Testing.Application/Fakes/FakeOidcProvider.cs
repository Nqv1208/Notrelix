using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// A company identity provider for tests: serves its key set at <c>{issuer}/jwks</c> and exchanges the one authorization
/// code at <c>{issuer}/token</c> for an ID token signed with a real RSA (or EC) key. Use it as the primary handler of the
/// guarded named HTTP client, or call <see cref="BuildIdToken"/> and <see cref="JwksJson"/> directly. Every knob that makes
/// a token wrong (issuer, audience, nonce, expiry, algorithm, key id, signing key) is a property.
/// </summary>
public sealed class FakeOidcProvider : HttpMessageHandler
{
    private readonly string _clientSecret;
    private readonly List<(string Method, string Url, string? Authorization, string Body)> _requests = [];

    public FakeOidcProvider(string issuer, string clientId, string clientSecret)
    {
        Issuer = issuer;
        ClientId = clientId;
        _clientSecret = clientSecret;
        Key = RSA.Create(2048);
    }

    public string Issuer { get; }
    public string ClientId { get; }
    public RSA Key { get; set; }

    /// <summary>Signs the token with this key instead of <see cref="Key"/> while the published key set stays the provider's own.</summary>
    public RSA? SigningKeyOverride { get; set; }
    public string KeyId { get; set; } = "key-1";

    public string Code { get; set; } = "auth-code-1";
    public string Subject { get; set; } = "subject-1";
    public string? Email { get; set; }
    public bool EmailVerified { get; set; } = true;
    public string? Name { get; set; }

    /// <summary>The nonce and PKCE challenge of the sign-in being answered; <see cref="Follow"/> reads them from the authorization address.</summary>
    public string? Nonce { get; set; }
    public string? CodeChallenge { get; set; }
    public string? RedirectUri { get; set; }
    public string? State { get; private set; }

    public string? IssuerClaim { get; set; }
    public object? AudienceClaim { get; set; }
    public string? AuthorizedParty { get; set; }
    public string? NonceClaim { get; set; }
    public TimeSpan ExpiresIn { get; set; } = TimeSpan.FromMinutes(5);
    public string Algorithm { get; set; } = "RS256";
    public bool IncludeKeyId { get; set; } = true;
    public Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.UtcNow;
    public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
    public bool OmitIdToken { get; set; }

    public IReadOnlyList<(string Method, string Url, string? Authorization, string Body)> Requests => _requests;

    /// <summary>Reads state, nonce and PKCE challenge from the authorization address the sign-in start produced.</summary>
    public FakeOidcProvider Follow(string authorizationUrl)
    {
        var query = Query(new Uri(authorizationUrl).Query);
        State = query["state"];
        Nonce = query["nonce"];
        CodeChallenge = query["code_challenge"];
        RedirectUri = query["redirect_uri"];
        return this;
    }

    public string JwksJson()
    {
        var parameters = Key.ExportParameters(false);
        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new { kty = "RSA", use = "sig", alg = "RS256", kid = KeyId, n = B64(parameters.Modulus!), e = B64(parameters.Exponent!) },
            },
        });
    }

    public string BuildIdToken() => BuildIdToken(Clock());

    public string BuildIdToken(DateTimeOffset now)
    {
        var header = new Dictionary<string, object> { ["alg"] = Algorithm, ["typ"] = "JWT" };
        if (IncludeKeyId) header["kid"] = KeyId;

        var payload = new Dictionary<string, object?>
        {
            ["iss"] = IssuerClaim ?? Issuer,
            ["aud"] = AudienceClaim ?? ClientId,
            ["sub"] = Subject,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = (now + ExpiresIn).ToUnixTimeSeconds(),
            ["nonce"] = NonceClaim ?? Nonce,
        };
        if (AuthorizedParty is not null) payload["azp"] = AuthorizedParty;
        if (Email is not null) payload["email"] = Email;
        payload["email_verified"] = EmailVerified;
        if (Name is not null) payload["name"] = Name;

        var signingInput = $"{B64(JsonSerializer.SerializeToUtf8Bytes(header))}.{B64(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        return Algorithm switch
        {
            "none" => signingInput + ".",
            "HS256" => $"{signingInput}.{B64(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_clientSecret), Encoding.ASCII.GetBytes(signingInput)))}",
            _ => $"{signingInput}.{B64((SigningKeyOverride ?? Key).SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))}",
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Add((request.Method.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
        var path = request.RequestUri.AbsolutePath;

        if (request.Method == HttpMethod.Get && path.EndsWith("/jwks", StringComparison.Ordinal))
            return Json(HttpStatusCode.OK, JwksJson());

        if (request.Method == HttpMethod.Post && path.EndsWith("/token", StringComparison.Ordinal))
        {
            if (TokenStatus != HttpStatusCode.OK) return Json(TokenStatus, "{\"error\":\"invalid_client\"}");

            var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(ClientId)}:{Uri.EscapeDataString(_clientSecret)}"));
            var form = Query(body);
            var verifierOk = CodeChallenge is null
                || (form.TryGetValue("code_verifier", out var verifier)
                    && B64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))) == CodeChallenge);
            if (request.Headers.Authorization?.Parameter != expected
                || form.GetValueOrDefault("code") != Code
                || form.GetValueOrDefault("grant_type") != "authorization_code"
                || (RedirectUri is not null && form.GetValueOrDefault("redirect_uri") != RedirectUri)
                || !verifierOk)
                return Json(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}");

            return Json(HttpStatusCode.OK, OmitIdToken
                ? "{\"access_token\":\"x\",\"token_type\":\"Bearer\"}"
                : JsonSerializer.Serialize(new { id_token = BuildIdToken(), access_token = "access-never-used", token_type = "Bearer" }));
        }

        return Json(HttpStatusCode.NotFound, "{}");
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static Dictionary<string, string> Query(string text) =>
        text.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length > 1 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : "");

    private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
