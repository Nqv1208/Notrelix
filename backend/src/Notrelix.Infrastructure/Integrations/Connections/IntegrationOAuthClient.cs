using System.Text.Json;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Infrastructure.Integrations.Connections;

/// <summary>
/// The provider side of the OAuth redirect flow for Google Calendar and Slack. The client id and secret come
/// from configuration; the code is redeemed server to server and the tokens go straight to the secret store.
/// Nothing here logs a token, a code or the client secret.
/// </summary>
internal sealed class IntegrationOAuthClient : IIntegrationOAuthClient
{
    private readonly IntegrationProviderSettings _settings;
    private readonly HttpClient _http;
    private readonly ILogger<IntegrationOAuthClient> _logger;

    public IntegrationOAuthClient(
        IntegrationProviderSettings settings, HttpClient http, ILogger<IntegrationOAuthClient> logger)
    {
        _settings = settings;
        _http = http;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(IntegrationProvider provider, string redirectUri, string state)
    {
        var options = Require(provider);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["state"] = state,
        };
        if (provider == IntegrationProvider.Slack)
        {
            query["scope"] = string.Join(',', options.Scopes);
        }
        else
        {
            query["scope"] = string.Join(' ', options.Scopes);
            query["access_type"] = "offline";
            query["prompt"] = "consent";
        }

        var separator = options.AuthorizationEndpoint.Contains('?') ? '&' : '?';
        return options.AuthorizationEndpoint + separator + string.Join('&',
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));
    }

    public async Task<IntegrationOAuthTokens?> ExchangeCodeAsync(
        IntegrationProvider provider, string code, string redirectUri, CancellationToken cancellationToken)
    {
        var options = Require(provider);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!,
            }),
        };

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("The {Provider} token endpoint answered {Status}", provider, (int)response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (provider == IntegrationProvider.Slack
                && !(root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True))
            {
                _logger.LogWarning("The Slack token endpoint refused the authorization");
                return null;
            }

            if (!root.TryGetProperty("access_token", out var accessToken) || accessToken.GetString() is not { Length: > 0 } access)
            {
                return null;
            }

            var refresh = root.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
            var scopes = root.TryGetProperty("scope", out var scope) && scope.GetString() is { } granted
                ? granted.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                : options.Scopes;
            var account = provider == IntegrationProvider.Slack
                && root.TryGetProperty("team", out var team)
                && team.TryGetProperty("id", out var teamId)
                    ? teamId.GetString()
                    : null;
            return new IntegrationOAuthTokens(access, refresh, account, scopes);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "The {Provider} token endpoint could not be reached", provider);
            return null;
        }
    }

    private IntegrationProviderOptions Require(IntegrationProvider provider)
        => _settings.For(provider) is { IsConfigured: true } options
            ? options
            : throw new InvalidOperationException($"Integration provider '{provider}' is not configured.");
}
