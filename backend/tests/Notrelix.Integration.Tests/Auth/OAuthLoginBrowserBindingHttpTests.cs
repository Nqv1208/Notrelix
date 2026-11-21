using Notrelix.Testing.Core;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.SharedKernel;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// A provider sign-in only completes in the browser that started it, over the real HTTP production graph with the
/// real state store (Redis) and a real database. The API connects as the application role, not the owner. Only the
/// provider is faked: it hands back one fixed Google identity for any code and never touches the network.
/// </summary>
[Collection("Cache")]
public sealed class OAuthLoginBrowserBindingHttpTests : IAsyncLifetime
{
    private const string Subject = "google-subject-attacker";
    private const string Email = "attacker@example.com";

    private readonly CacheTestContainer _fixture;

    public OAuthLoginBrowserBindingHttpTests(CacheTestContainer fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> CreateFactory()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), "notrelix-oauth-binding-keys", Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
            ["ConnectionStrings:NotrelixWorkerDb"] = _fixture.PostgresWorkerConnectionString,
            ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
            ["JwtSettings:SecretKey"] = "oauth-binding-test-signing-key-ThisIsNotASecret-0123456789",
            ["JwtSettings:Issuer"] = "https://notrelix.test",
            ["JwtSettings:Audience"] = "notrelix-api",
            ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
            ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "587",
            ["Smtp:FromEmail"] = "graph@notrelix.test",
            ["DataProtection:PersistKeys"] = "true",
            ["DataProtection:KeysPath"] = keysPath,
            ["Messaging:Transport"] = "InMemory",
            ["Billing:Mode"] = "Database",
            ["Storage:Mode"] = "Local",
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
        };

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            builder.UseEnvironment("Production");

            foreach (var (rateLimitKey, rateLimitValue) in RateLimitTestSettings.Generous)
            {
                builder.UseSetting(rateLimitKey, rateLimitValue);
            }

            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOAuthOptionsProvider>();
                services.AddScoped<IOAuthOptionsProvider, FakeOAuthOptions>();
                services.RemoveAll<IOAuthProviderClient>();
                services.AddScoped<IOAuthProviderClient, FakeGoogle>();
            });
        });
    }

    private static HttpClient Browser(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("http://localhost"),
        });

    /// <summary>Starts a sign-in the way a browser does: the state comes from the provider address, the binding from the cookie.</summary>
    private static async Task<(string State, string Cookie)> StartAsync(HttpClient browser)
    {
        var response = await browser.GetAsync("/api/v1/auth/oauth/google/start");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var state = System.Web.HttpUtility.ParseQueryString(response.Headers.Location!.Query)["state"]!;
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("oauthBinding=", StringComparison.Ordinal));
        return (state, setCookie[..setCookie.IndexOf(';')]);
    }

    private static Task<HttpResponseMessage> CallbackAsync(HttpClient browser, string state, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/auth/oauth/google/callback?code=attacker-code&state={Uri.EscapeDataString(state)}");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return browser.SendAsync(request);
    }

    private static string[] SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    private static void ShouldHaveOpenedASession(HttpResponseMessage response)
    {
        response.Headers.Location!.OriginalString.Should().Be("/auth/callback");
        SetCookies(response).Should().Contain(c => c.StartsWith("accessToken=")).And.Contain(c => c.StartsWith("refreshToken="));
    }

    private static void ShouldHaveBeenRefusedAsAnInvalidState(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.state-invalid");
        SetCookies(response).Should().NotContain(c => c.StartsWith("accessToken=") || c.StartsWith("refreshToken="));
    }

    private static void ShouldHaveClearedTheBinding(HttpResponseMessage response) =>
        SetCookies(response).Should().Contain(c =>
            c.StartsWith("oauthBinding=;", StringComparison.Ordinal) && c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    private async Task<int> OAuthAccountCountAsync()
    {
        await using var context = _fixture.CreatePostgresOwnerContext();
        return await context.OAuthAccounts.IgnoreQueryFilters().CountAsync();
    }

    [Fact]
    public async Task TheBrowserThatStartedTheSignIn_Completes_ItOnce()
    {
        using var factory = CreateFactory();
        using var browser = Browser(factory);

        var (state, cookie) = await StartAsync(browser);
        var response = await CallbackAsync(browser, state, cookie);

        ShouldHaveOpenedASession(response);
        ShouldHaveClearedTheBinding(response);
        (await OAuthAccountCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ALoginCsrf_AttackerStateInTheVictimsBrowserWithNoCookie_IsRefused()
    {
        using var factory = CreateFactory();
        using var attacker = Browser(factory);
        using var victim = Browser(factory);
        var (attackerState, _) = await StartAsync(attacker);

        // The attacker gets the victim's browser to load the callback address that carries the attacker's state and code.
        var response = await CallbackAsync(victim, attackerState, cookie: null);

        ShouldHaveBeenRefusedAsAnInvalidState(response);
        ShouldHaveClearedTheBinding(response);
        (await OAuthAccountCountAsync()).Should().Be(0, "no one was signed in or registered");
    }

    [Fact]
    public async Task ALoginCsrf_AVictimCookieFromAnotherStart_IsRefused()
    {
        using var factory = CreateFactory();
        using var attacker = Browser(factory);
        using var victim = Browser(factory);
        var (attackerState, _) = await StartAsync(attacker);
        var (_, victimCookie) = await StartAsync(victim);

        var response = await CallbackAsync(victim, attackerState, victimCookie);

        ShouldHaveBeenRefusedAsAnInvalidState(response);
        ShouldHaveClearedTheBinding(response);
        (await OAuthAccountCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ARefusedCallback_UsesUpTheState_SoTheRightBrowserCannotSpendItAfterwards()
    {
        using var factory = CreateFactory();
        using var browser = Browser(factory);
        var (state, cookie) = await StartAsync(browser);

        var refused = await CallbackAsync(browser, state, cookie: null);
        var retry = await CallbackAsync(browser, state, cookie);

        ShouldHaveBeenRefusedAsAnInvalidState(refused);
        ShouldHaveBeenRefusedAsAnInvalidState(retry);
        (await OAuthAccountCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AReplayedState_IsRefused_AndTheCookieIsCleared()
    {
        using var factory = CreateFactory();
        using var browser = Browser(factory);
        var (state, cookie) = await StartAsync(browser);
        ShouldHaveOpenedASession(await CallbackAsync(browser, state, cookie));

        var replay = await CallbackAsync(browser, state, cookie);

        ShouldHaveBeenRefusedAsAnInvalidState(replay);
        ShouldHaveClearedTheBinding(replay);
    }

    private sealed class FakeOAuthOptions : IOAuthOptionsProvider
    {
        public bool IsProviderEnabled(OAuthProvider provider) => provider == OAuthProvider.Google;

        public string GetRedirectUri(OAuthProvider provider) => "http://localhost/api/v1/auth/oauth/google/callback";

        public string GetFrontendSuccessUrl() => "/auth/callback";

        public string GetFrontendFailureUrl() => "/login";

        public string[] GetAllowedReturnOrigins() => [];
    }

    private sealed class FakeGoogle : IOAuthProviderClient
    {
        public Task<OAuthAuthorizationUrlResult> BuildAuthorizationUrlAsync(
            OAuthProvider provider, OAuthAuthorizationRequest request, CancellationToken ct) =>
            Task.FromResult(new OAuthAuthorizationUrlResult(
                $"https://accounts.google.test/o/oauth2/auth?state={Uri.EscapeDataString(request.State)}"));

        public Task<ExternalOAuthProfile> RedeemCodeAsync(
            OAuthProvider provider, OAuthCodeRedemptionRequest request, CancellationToken ct) =>
            Task.FromResult(new ExternalOAuthProfile(
                provider, Subject, Email, EmailVerified: true, "Attacker", null, JsonValue.EmptyObject()));
    }
}
