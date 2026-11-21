using System.Net.Http.Headers;
using System.Text.Json;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Infrastructure.Identity.OAuth;
using Notrelix.Infrastructure.Oidc;

namespace Notrelix.Infrastructure.Identity.Sso;

/// <summary>
/// The code exchange and the ID token check of a company sign-in. Every address comes from the provider's discovery
/// document, read now from the configured issuer; every request goes through the guarded outbound client (https,
/// resolved addresses checked and pinned, no redirects, 5 seconds). Responses are cut off at 256 KB. The client secret
/// travels only in the Basic authorization header of the token request and is never put in a message or a log line.
/// </summary>
public sealed class SsoCodeRedeemer : ISsoCodeRedeemer
{
    public const int MaxResponseBytes = 256 * 1024;

    private readonly IHttpClientFactory _factory;
    private readonly IOidcDiscoveryClient _discovery;
    private readonly IDateTimeProvider _clock;

    public SsoCodeRedeemer(IHttpClientFactory factory, IOidcDiscoveryClient discovery, IDateTimeProvider clock)
    {
        _factory = factory;
        _discovery = discovery;
        _clock = clock;
    }

    public async Task<SsoIdentityClaims> RedeemAsync(SsoCodeRedemption redemption, CancellationToken cancellationToken)
    {
        var discovered = await _discovery.DiscoverAsync(redemption.Issuer, cancellationToken);
        if (discovered.Document is not { } document)
            throw new SsoRedemptionException($"discovery failed ({discovered.Failure})");

        using var client = _factory.CreateClient(OidcGuardedHttp.ClientName);
        var idToken = await ExchangeCodeAsync(client, document.TokenEndpoint, redemption, cancellationToken);
        var keys = await FetchKeysAsync(client, document.JwksUri, cancellationToken);

        JsonDocument claims;
        try
        {
            claims = SsoIdTokenValidator.Validate(idToken, keys, document.Issuer, redemption.ClientId, redemption.Nonce, _clock.UtcNow);
        }
        catch (OidcIdTokenException ex)
        {
            throw new SsoRedemptionException(ex.Message);
        }

        using (claims)
        {
            var root = claims.RootElement;
            return new SsoIdentityClaims(
                OidcIdTokenValidator.StringClaim(root, "sub")!,
                OidcIdTokenValidator.StringClaim(root, "email"),
                IsTrue(root, "email_verified"),
                OidcIdTokenValidator.StringClaim(root, "name"));
        }
    }

    private static async Task<string> ExchangeCodeAsync(
        HttpClient client, string tokenEndpoint, SsoCodeRedemption redemption, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = redemption.Code,
                ["redirect_uri"] = redemption.RedirectUri,
                ["code_verifier"] = redemption.CodeVerifier,
            }),
        };
        // client_secret_basic (RFC 6749 2.3.1): both values are form-urlencoded before they are joined.
        var credentials = $"{Uri.EscapeDataString(redemption.ClientId)}:{Uri.EscapeDataString(redemption.ClientSecret)}";
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var body = await SendAsync(client, request, "token endpoint", cancellationToken);
        try
        {
            using var json = JsonDocument.Parse(body);
            var idToken = json.RootElement.ValueKind == JsonValueKind.Object
                ? OidcIdTokenValidator.StringClaim(json.RootElement, "id_token")
                : null;
            return string.IsNullOrWhiteSpace(idToken) ? throw new SsoRedemptionException("token response has no id token") : idToken;
        }
        catch (JsonException)
        {
            throw new SsoRedemptionException("token response unreadable");
        }
    }

    private static async Task<JsonWebKeySet> FetchKeysAsync(HttpClient client, string jwksUri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, jwksUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        var body = await SendAsync(client, request, "key set", cancellationToken);
        try
        {
            return new JsonWebKeySet(body);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            throw new SsoRedemptionException("key set unreadable");
        }
    }

    private static async Task<string> SendAsync(
        HttpClient client, HttpRequestMessage request, string what, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new SsoRedemptionException($"{what} answered {(int)response.StatusCode}");
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new SsoRedemptionException($"{what} response too large");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaxResponseBytes)
                    throw new SsoRedemptionException($"{what} response too large");
            }

            return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new SsoRedemptionException($"{what} unreachable");
        }
    }

    private static bool IsTrue(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
        && (value.ValueKind == JsonValueKind.True
            || (value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase)));
}
