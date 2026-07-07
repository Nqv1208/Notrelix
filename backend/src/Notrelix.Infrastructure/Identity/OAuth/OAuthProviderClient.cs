using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Infrastructure.Identity.OAuth;

public sealed class OAuthProviderClient : IOAuthProviderClient
{
    private readonly OAuthOptions _options;
    private readonly HttpClient _httpClient;

    public OAuthProviderClient(
        IOptions<OAuthOptions> options,
        HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public Task<OAuthAuthorizationUrlResult> BuildAuthorizationUrlAsync(
        OAuthProvider provider,
        OAuthAuthorizationRequest request,
        CancellationToken ct)
    {
        var config = _options.GetProviderConfig(provider);
        var scopeSeparator = provider == OAuthProvider.GitHub ? "," : " ";
        var scopes = string.Join(scopeSeparator, config.Scopes);

        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["scope"] = scopes,
            ["state"] = request.State,
        };

        if (provider == OAuthProvider.Google)
        {
            queryParams["response_type"] = "code";
            queryParams["access_type"] = "offline";
            queryParams["prompt"] = "consent";

            if (!string.IsNullOrWhiteSpace(request.Nonce))
                queryParams["nonce"] = request.Nonce;

            if (!string.IsNullOrWhiteSpace(request.CodeChallenge))
            {
                queryParams["code_challenge"] = request.CodeChallenge;
                queryParams["code_challenge_method"] = request.CodeChallengeMethod ?? "S256";
            }
        }

        if (provider == OAuthProvider.GitHub)
        {
            queryParams["allow_signup"] = "true";
        }

        var queryString = string.Join("&",
            queryParams.Where(p => p.Value is not null)
                .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));

        var url = $"{config.AuthorizationEndpoint}?{queryString}";
        return Task.FromResult(new OAuthAuthorizationUrlResult(url));
    }

    public async Task<ExternalOAuthProfile> RedeemCodeAsync(
        OAuthProvider provider,
        OAuthCodeRedemptionRequest request,
        CancellationToken ct)
    {
        var config = _options.GetProviderConfig(provider);

        var formFields = new List<KeyValuePair<string, string>>
        {
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret),
            new("code", request.Code),
            new("redirect_uri", request.RedirectUri),
        };

        if (provider == OAuthProvider.Google && !string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            formFields.Add(new("code_verifier", request.CodeVerifier));
        }

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formFields)
        };

        if (provider == OAuthProvider.GitHub)
        {
            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        var tokenResponse = await _httpClient.SendAsync(httpRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Token endpoint returned empty response.");

        if (provider == OAuthProvider.Google)
        {
            var idToken = tokenJson.RootElement.TryGetProperty("id_token", out var idTokenEl)
                ? idTokenEl.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(idToken))
            {
                return ValidateIdTokenAndBuildProfile(
                    provider, idToken, request.Nonce);
            }
        }

        var accessToken = tokenJson.RootElement.TryGetProperty("access_token", out var accessTokenEl)
            ? accessTokenEl.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return await FetchUserInfoAndBuildProfile(
                provider, config, accessToken, ct);
        }

        throw new InvalidOperationException("Token endpoint returned neither id_token nor access_token.");
    }

    private static ExternalOAuthProfile ValidateIdTokenAndBuildProfile(
        OAuthProvider provider,
        string idToken,
        string? nonce)
    {
        using var document = DecodeJwtPayload(idToken);

        var subject = document.RootElement.GetProperty("sub").GetString()
            ?? throw new InvalidOperationException("ID token missing 'sub' claim.");

        var email = document.RootElement.TryGetProperty("email", out var emailEl)
            ? emailEl.GetString()
            : null;

        var emailVerified = document.RootElement.TryGetProperty("email_verified", out var verifiedEl)
            ? verifiedEl.GetBoolean()
            : false;

        var name = document.RootElement.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString()
            : null;

        var picture = document.RootElement.TryGetProperty("picture", out var pictureEl)
            ? pictureEl.GetString()
            : null;

        return new ExternalOAuthProfile(
            Provider: provider,
            Subject: subject,
            Email: email,
            EmailVerified: emailVerified,
            Name: name,
            AvatarUrl: picture,
            RawProfile: JsonValue.Create(document.RootElement.GetRawText()));
    }

    private async Task<ExternalOAuthProfile> FetchUserInfoAndBuildProfile(
        OAuthProvider provider,
        OAuthProviderConfig config,
        string accessToken,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, config.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (provider == OAuthProvider.GitHub)
        {
            request.Headers.UserAgent.TryParseAdd("Notrelix-App");
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
            ?? throw new InvalidOperationException("UserInfo endpoint returned empty response.");

        var root = json.RootElement;

        var subject = root.TryGetProperty("id", out var idEl)
            ? GetStringOrNumberAsString(idEl)
            : root.TryGetProperty("sub", out var subEl)
                ? subEl.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(subject))
            throw new InvalidOperationException("UserInfo response missing subject identifier.");

        var email = GetStringOrNull(root, "email");
        var name = GetStringOrNull(root, "name") ?? GetStringOrNull(root, "login");
        var picture = GetStringOrNull(root, "avatar_url") ?? GetStringOrNull(root, "picture");

        // GitHub email fallback: fetch from /user/emails if email is null
        if (string.IsNullOrWhiteSpace(email) && provider == OAuthProvider.GitHub
            && !string.IsNullOrWhiteSpace(config.EmailsEndpoint))
        {
            email = await FetchPrimaryVerifiedGitHubEmailAsync(config.EmailsEndpoint, accessToken, ct);
        }

        var emailVerified = provider == OAuthProvider.GitHub
            ? !string.IsNullOrWhiteSpace(email)
            : root.TryGetProperty("email_verified", out var verifiedEl) && verifiedEl.GetBoolean();

        return new ExternalOAuthProfile(
            Provider: provider,
            Subject: subject!,
            Email: email,
            EmailVerified: emailVerified,
            Name: name,
            AvatarUrl: picture,
            RawProfile: JsonValue.Create(root.GetRawText()));
    }

    private static JsonDocument DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("Invalid JWT format.");

        var payload = parts[1];
        var padding = (4 - payload.Length % 4) % 4;
        var padded = padding switch
        {
            2 => payload + "==",
            1 => payload + "=",
            _ => payload
        };
        var bytes = Convert.FromBase64String(padded);
        return JsonDocument.Parse(bytes);
    }

    private static string? GetStringOrNumberAsString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue.ToString(CultureInfo.InvariantCulture)
                : element.GetRawText(),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static string? GetStringOrNull(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private async Task<string?> FetchPrimaryVerifiedGitHubEmailAsync(
        string emailsEndpoint,
        string accessToken,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, emailsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.TryParseAdd("Notrelix-App");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        if (json is null)
        {
            return null;
        }

        foreach (var emailObj in json.RootElement.EnumerateArray())
        {
            var isPrimary = emailObj.TryGetProperty("primary", out var primaryEl)
                && primaryEl.GetBoolean();
            var isVerified = emailObj.TryGetProperty("verified", out var verifiedEl)
                && verifiedEl.GetBoolean();
            var email = emailObj.TryGetProperty("email", out var emailEl)
                ? emailEl.GetString()
                : null;

            if (isPrimary && isVerified && !string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }

        return null;
    }
}
