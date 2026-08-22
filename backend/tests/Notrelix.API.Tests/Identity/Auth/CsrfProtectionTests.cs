using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Notrelix.API.Tests.Contracts;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Tests.Identity.Auth;

public class CsrfProtectionTests
{
    private const string BootstrapPath = "/api/v1/auth/csrf";
    private const string LoginPath = "/api/v1/auth/login";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Bootstrap_ReturnsTokenInBody_AndSetsMatchingHttpOnlyCookie()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(BootstrapPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IssueCsrfTokenResponse>(Json);
        body!.Token.Should().NotBeNullOrWhiteSpace();

        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("csrf_token="));
        var cookieValue = Uri.UnescapeDataString(ExtractCookieValue(setCookie));
        cookieValue.Should().Be(body.Token);

        var normalizedCookie = setCookie.ToLowerInvariant();
        normalizedCookie.Should().Contain("httponly",
            "the CSRF cookie must not be JavaScript-readable under ADR-005");
        normalizedCookie.Should().Contain("path=/");

        // Testing environment is not production: Lax + non-Secure mirrors the auth cookie policy.
        normalizedCookie.Should().Contain("samesite=lax");
        normalizedCookie.Should().NotContain("; secure");
    }

    [Fact]
    public async Task Production_Protector_UsesSecureNoneCookiePolicy()
    {
        var environment = new HostEnvironmentStub { EnvironmentName = Environments.Production };
        var protector = new CsrfProtector(environment);
        var context = new DefaultHttpContext();

        protector.SetCookie(context, "token-value");

        var setCookie = context.Response.Headers["Set-Cookie"].ToString();
        var normalizedCookie = setCookie.ToLowerInvariant();
        normalizedCookie.Should().Contain("httponly");
        normalizedCookie.Should().Contain("secure");
        normalizedCookie.Should().Contain("samesite=none",
            "cross-origin browser mutations require the CSRF cookie to travel with them");
        normalizedCookie.Should().Contain("path=/");
    }

    [Fact]
    public async Task SafeRequest_IsValidatedWithoutCsrfMaterial()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        // The bootstrap itself is a safe GET and must be callable with no
        // pre-existing token (no bootstrap chicken-and-egg).
        var response = await client.GetAsync(BootstrapPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnsafeAmbientRequest_WithoutToken_GetsCanonicalProblemDetails()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginPath, new { email = "a@b.c", password = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("status").GetInt32().Should().Be(403);
        problem.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        problem.GetProperty("errorCode").GetString().Should().Be("security.csrf_validation_failed");
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
        problem.GetProperty("type").GetString().Should().Contain("csrf-validation-failed");
    }

    [Fact]
    public async Task UnsafeAmbientRequest_WithValidPair_ContinuesToHandler()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var bootstrap = await client.GetAsync(BootstrapPath);
        var token = (await bootstrap.Content.ReadFromJsonAsync<IssueCsrfTokenResponse>(Json))!.Token;

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
        };
        request.Headers.Add("Cookie", $"csrf_token={token}");
        request.Headers.Add(CsrfProtector.HeaderName, token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "a valid Double Submit pair must not be rejected by the CSRF gate");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the request continues to the mocked login handler after passing CSRF");
    }

    [Fact]
    public async Task UnsafeAmbientRequest_WithMismatchedPair_IsRejected()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
        };
        request.Headers.Add("Cookie", "csrf_token=cookie-value");
        request.Headers.Add(CsrfProtector.HeaderName, "header-value");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("errorCode").GetString().Should().Be("security.csrf_validation_failed");
    }

    [Fact]
    public async Task UnsafeAmbientRequest_WithMissingCookieOnly_IsRejected()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
        };
        request.Headers.Add(CsrfProtector.HeaderName, "some-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnsafeAmbientRequest_WithMissingHeaderOnly_IsRejected()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
        };
        request.Headers.Add("Cookie", "csrf_token=some-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnsafeRequest_WithAuthorizationCredential_IsOutsideBrowserCsrfGate()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "explicit-credential");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "an explicitly presented Authorization credential is a non-ambient principal " +
            "and must never be rejected for missing browser CSRF material");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the request reaches the handler instead of the CSRF gate");
    }

    [Fact]
    public async Task FlagDisabled_MiddlewareDoesNotInterfereWithUnsafeRequests()
    {
        await using var factory = new NotrelixApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginPath, new { email = "a@b.c", password = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "with Security:Csrf:Enabled=false the middleware must not reject anything");
    }

    [Fact]
    public async Task Classifier_UnsafeMethodWithoutAuthorization_IsApplicable()
    {
        var classifier = new CsrfApplicabilityClassifier();

        classifier.IsBrowserCsrfApplicable(CreateHttpContext("POST", authorization: null).Request).Should().BeTrue();
        classifier.IsBrowserCsrfApplicable(CreateHttpContext("PUT", authorization: null).Request).Should().BeTrue();
        classifier.IsBrowserCsrfApplicable(CreateHttpContext("PATCH", authorization: null).Request).Should().BeTrue();
        classifier.IsBrowserCsrfApplicable(CreateHttpContext("DELETE", authorization: null).Request).Should().BeTrue();
    }

    [Fact]
    public async Task Classifier_SafeMethods_And_AuthorizationCredentialedRequests_AreNotApplicable()
    {
        var classifier = new CsrfApplicabilityClassifier();

        classifier.IsBrowserCsrfApplicable(CreateHttpContext("GET").Request).Should().BeFalse();
        classifier.IsBrowserCsrfApplicable(CreateHttpContext("HEAD").Request).Should().BeFalse();
        classifier.IsBrowserCsrfApplicable(CreateHttpContext("OPTIONS").Request).Should().BeFalse();

        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
        {
            classifier.IsBrowserCsrfApplicable(CreateHttpContext(method, authorization: "Bearer ntk_v1.abc").Request)
                .Should().BeFalse(
                    $"{method} with an explicit Authorization credential is outside the browser CSRF threat model");
        }
    }

    private static DefaultHttpContext CreateHttpContext(string method, string? authorization = "Bearer x")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = new(authorization);
        }
        return context;
    }

    private static string ExtractCookieValue(string setCookieHeader) =>
        setCookieHeader.Split(';', 2)[0].Split('=', 2)[1];

    private sealed record IssueCsrfTokenResponse(string Token);

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Notrelix.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "/";
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
