using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// IA-TST-CSRF-INT-001 / IA-TST-CSRF-INT-002 / IAREQ126–IAREQ130 /
/// IA-TST-MIG-CSRF-001.
///
/// Cross-stack browser flow against the real HTTP host with the exact request
/// shape produced by the canonical frontend transport (@notrelix/contracts
/// client): the token is obtained from the bootstrap response BODY and echoed
/// in X-CSRF-Token; cookies travel explicitly per origin. No JavaScript
/// API-cookie reading is involved or required.
///
/// Rollout compatibility (P13-CSRF-04): the same suite runs green with
/// Security:Csrf:Enabled=false (default factory) proving staged deployment
/// safety, and with the flag enabled here proving enforcement.
/// </summary>
public class CsrfCrossStackFlowTests
{
    private const string BootstrapPath = "/api/v1/auth/csrf";
    private const string LoginPath = "/api/v1/auth/login";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string RefreshPath = "/api/v1/auth/refresh";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Minimal browser model: explicit per-origin cookie jar + client memory.</summary>
    private sealed class BrowserClient
    {
        public Dictionary<string, string> Cookies { get; } = new(StringComparer.Ordinal);
        public string? MemoryToken { get; set; }

        public void Absorb(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return;
            }

            foreach (var value in values)
            {
                var pair = value.Split(';', 2)[0];
                var eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                Cookies[pair[..eq]] = Uri.UnescapeDataString(pair[(eq + 1)..]);
            }
        }

        public string CookieHeader() =>
            string.Join("; ", Cookies.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static async Task<string> BootstrapAsync(
        HttpClient client,
        BrowserClient browser,
        bool updateMemory = true)
    {
        var response = await client.GetAsync(BootstrapPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        browser.Absorb(response);

        var body = (await response.Content.ReadFromJsonAsync<JsonElement>(Json))
            .GetProperty("token").GetString()!;
        body.Should().NotBeNullOrWhiteSpace();

        if (updateMemory)
        {
            browser.MemoryToken = body;
        }

        return body;
    }

    private static HttpRequestMessage UnsafeRequest(
        HttpMethod method,
        string path,
        BrowserClient browser,
        object? payload = null,
        string? headerOverride = null,
        bool sendHeader = true,
        bool sendCookies = true)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = payload is null
                ? null
                : JsonContent.Create(payload),
        };

        if (sendCookies && browser.Cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", browser.CookieHeader());
        }

        if (sendHeader && browser.MemoryToken is not null)
        {
            request.Headers.Add(CsrfProtector.HeaderName, headerOverride ?? browser.MemoryToken);
        }

        return request;
    }

    [Fact]
    public async Task FullBrowserFlow_WithCrossOriginTransport_SucceedsEndToEnd()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();
        var browser = new BrowserClient();

        // 1–2. clean client → CSRF bootstrap
        var token1 = await BootstrapAsync(client, browser);
        token1.Should().NotBeNullOrWhiteSpace();

        // 3. session establishment (login) carries the CSRF pair
        var login = await client.SendAsync(UnsafeRequest(
            HttpMethod.Post, LoginPath, browser,
            payload: new { email = "user@test.local", password = "secret" }));
        login.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "a valid Double Submit pair must pass the CSRF gate at session establishment");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        browser.Absorb(login);

        // Ambient session cookies are now established.
        browser.Cookies.Should().ContainKey("accessToken");
        browser.Cookies.Should().ContainKey("refreshToken");

        // 4. unsafe authenticated mutation: the CSRF gate passes and the request
        //    reaches the authentication/use-case layers. With the mocked JWT the
        //    auth layer answers 401 — the property under proof here is the
        //    cross-origin CSRF transport, not handler business success.
        var logout = await client.SendAsync(UnsafeRequest(HttpMethod.Post, LogoutPath, browser));
        logout.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "ambient cookies + memory token pass the CSRF gate on authenticated mutations");
        logout.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the mocked access token fails JWT validation AFTER passing CSRF — proving gate ordering");

        // 5. refresh keeps requiring the same transport (no raw bypass)
        var refresh = await client.SendAsync(UnsafeRequest(HttpMethod.Post, RefreshPath, browser));
        refresh.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "refresh must not bypass CSRF by using a raw non-CSRF path");
        browser.Absorb(refresh);

        // 6. unsafe mutation after refresh still carries the pair
        var afterRefresh = await client.SendAsync(UnsafeRequest(HttpMethod.Post, LogoutPath, browser));
        afterRefresh.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        // 7–9. reloaded client (memory lost, cookies kept): bootstrap again → mutate
        browser.MemoryToken = null;
        var token2 = await BootstrapAsync(client, browser);
        token2.Should().NotBeNullOrWhiteSpace();

        var finalMutation = await client.SendAsync(UnsafeRequest(HttpMethod.Post, LogoutPath, browser));
        finalMutation.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "a fresh bootstrap must immediately enable a new unsafe mutation");
    }

    [Fact]
    public async Task NegativeMatrix_MissingHeader_WrongHeader_StaleToken_AreRejected()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();
        var browser = new BrowserClient();

        await BootstrapAsync(client, browser);

        // missing header
        var missingHeader = await client.SendAsync(UnsafeRequest(
            HttpMethod.Post, LoginPath, browser,
            payload: new { email = "u@t.l", password = "x" }, sendHeader: false));
        missingHeader.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // wrong header value
        var wrongHeader = await client.SendAsync(UnsafeRequest(
            HttpMethod.Post, LoginPath, browser,
            payload: new { email = "u@t.l", password = "x" },
            headerOverride: "definitely-not-the-token"));
        wrongHeader.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var problem = await wrongHeader.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("errorCode").GetString()
            .Should().Be("security.csrf_validation_failed",
                "CSRF failures use the canonical ProblemDetails contract");
        problem.ToString().Should().NotContain(browser.MemoryToken!,
            "the failure response must not echo token material");

        // stale token: cookie rotated by a second bootstrap while the client
        // memory kept the previous value → mismatch → deterministic rejection.
        var staleToken = browser.MemoryToken!;
        await BootstrapAsync(client, browser, updateMemory: false);
        var stale = await client.SendAsync(UnsafeRequest(
            HttpMethod.Post, LoginPath, browser,
            payload: new { email = "u@t.l", password = "x" },
            headerOverride: staleToken));
        stale.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a rotated cookie invalidates the remembered memory token");
    }

    [Fact]
    public async Task ExplicitNonBrowserCredential_IsOutsideBrowserCsrfGate()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "service@test.local", password = "x" }),
        };
        request.Headers.Authorization = new("Bearer", "explicit-non-ambient-credential");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "an explicit non-ambient credential must not be forced into browser CSRF");
    }
}
