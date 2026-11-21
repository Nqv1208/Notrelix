using System.Text.Json;
using Notrelix.Application.Common.Oidc;

namespace Notrelix.Infrastructure.Oidc;

/// <inheritdoc cref="IOidcDiscoveryClient"/>
public sealed class OidcDiscoveryClient : IOidcDiscoveryClient
{
    public const int MaxDocumentBytes = 256 * 1024;

    private readonly IHttpClientFactory _factory;
    private readonly bool _allowPrivate;

    public OidcDiscoveryClient(IHttpClientFactory factory, IOptions<OidcOutboundOptions> options)
    {
        _factory = factory;
        _allowPrivate = options.Value.AllowPrivateTargets;
    }

    public async Task<OidcDiscoveryResult> DiscoverAsync(string issuer, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) || !string.IsNullOrEmpty(issuerUri.UserInfo)
            || !string.IsNullOrEmpty(issuerUri.Query) || !string.IsNullOrEmpty(issuerUri.Fragment))
            return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.IssuerInvalid);
        if (issuerUri.Scheme != Uri.UriSchemeHttps)
            return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.NotHttps);
        if (!OidcGuardedHttp.IsAllowedEndpoint(issuer, _allowPrivate))
            return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.AddressRefused);

        var url = issuer.TrimEnd('/') + "/.well-known/openid-configuration";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OidcGuardedHttp.Timeout);
        try
        {
            using var client = _factory.CreateClient(OidcGuardedHttp.ClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", "Notrelix-Sso/1");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            var status = (int)response.StatusCode;
            if (status is >= 300 and < 400) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.Redirected);
            if (!response.IsSuccessStatusCode) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.BadStatus);
            if (response.Content.Headers.ContentLength > MaxDocumentBytes) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.TooLarge);

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaxDocumentBytes) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.TooLarge);
            }

            return Parse(issuer, buffer.ToArray());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.Timeout);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            return OidcDiscoveryResult.Fail(Blocked(ex) ? OidcDiscoveryFailure.AddressRefused : OidcDiscoveryFailure.Unreachable);
        }
    }

    private OidcDiscoveryResult Parse(string issuer, byte[] body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.DocumentInvalid);

            var documentIssuer = Read(json.RootElement, "issuer");
            if (documentIssuer is null) return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.DocumentInvalid);
            // The document must speak for the issuer it was fetched from (RFC 8414 / OIDC Discovery 4.3).
            if (!string.Equals(documentIssuer, issuer, StringComparison.Ordinal))
                return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.IssuerMismatch);

            var authorization = Read(json.RootElement, "authorization_endpoint");
            var token = Read(json.RootElement, "token_endpoint");
            var jwks = Read(json.RootElement, "jwks_uri");
            if (authorization is null || token is null || jwks is null)
                return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.DocumentInvalid);
            if (!OidcGuardedHttp.IsAllowedEndpoint(authorization, _allowPrivate)
                || !OidcGuardedHttp.IsAllowedEndpoint(token, _allowPrivate)
                || !OidcGuardedHttp.IsAllowedEndpoint(jwks, _allowPrivate))
                return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.EndpointInvalid);

            return OidcDiscoveryResult.Ok(new OidcDiscoveryDocument(documentIssuer, authorization, token, jwks));
        }
        catch (JsonException)
        {
            return OidcDiscoveryResult.Fail(OidcDiscoveryFailure.DocumentInvalid);
        }
    }

    private static string? Read(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;

    private static bool Blocked(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            if (e is Integrations.Connections.BlockedWebhookAddressException) return true;
            if (e.InnerException is null) return false;
        }

        return false;
    }
}
