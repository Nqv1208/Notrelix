using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// The OAuth login callback never turns a multi-factor challenge into a session. An MFA-enabled
/// user gets the challenge in a short-lived httpOnly cookie scoped to the MFA endpoints (never in
/// a URL) and lands on the sign-in second step; a successful sign-in returns to the stored path
/// only when it is an internal root-relative one.
/// </summary>
public sealed class OAuthCallbackHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public Result<AuthResult> Outcome { get; set; } = Result<AuthResult>.Failure("not set");

    public string? StoredReturnUrl { get; set; }

    public OAuthCallbackHost()
    {
        // One host for the whole class (building a host is slow); each test sets the outcome first.
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
            services.AddScoped<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteOAuthLoginCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });

            services.RemoveAll<IOAuthStateStore>();
            services.AddScoped<IOAuthStateStore>(_ =>
            {
                var store = new Mock<IOAuthStateStore>();
                store.Setup(s => s.PeekAsync("s", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new OAuthLoginState(
                        "s", "nonce", "verifier", OAuthProvider.Google, StoredReturnUrl, DateTimeOffset.UtcNow.AddMinutes(10)));
                return store.Object;
            });
        }));
    }

    public HttpClient CreateClient(bool https = false) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(https ? "https://localhost" : "http://localhost"),
        });

    public void Dispose() => _factory.Dispose();
}

public class OAuthCallbackMfaTests : IClassFixture<OAuthCallbackHost>
{
    private const string CallbackPath = "/api/v1/auth/oauth/google/callback?code=c&state=s";

    private readonly OAuthCallbackHost _host;

    public OAuthCallbackMfaTests(OAuthCallbackHost host) => _host = host;

    private HttpClient CreateClient(Result<AuthResult> outcome, string? storedReturnUrl = null, bool https = false)
    {
        _host.Outcome = outcome;
        _host.StoredReturnUrl = storedReturnUrl;
        return _host.CreateClient(https);
    }

    private static Result<AuthResult> MfaOutcome(string token = "challenge-token", int minutes = 5) =>
        Result<AuthResult>.Success(new AuthResult
        {
            MfaRequired = true,
            MfaChallengeToken = token,
            MfaMethod = "AuthenticatorApp",
            MfaExpiresAt = DateTime.UtcNow.AddMinutes(minutes),
        });

    private static Result<AuthResult> SessionOutcome() =>
        Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });

    private static string[] SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    [Fact]
    public async Task Callback_ForMfaEnabledUser_HandsTheChallengeOverInAScopedHttpOnlyCookie()
    {
        using var client = CreateClient(MfaOutcome());

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var cookies = SetCookies(response);
        cookies.Should().ContainSingle("only the challenge cookie is set; there is no session yet");
        var cookie = cookies[0];
        cookie.Should().StartWith("mfaChallenge=challenge-token");
        cookie.Should().Contain("path=/api/v1/auth/mfa");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=lax");
        cookie.Should().NotContainEquivalentOf("secure", "the request is plain HTTP");
        var maxAge = int.Parse(System.Text.RegularExpressions.Regex.Match(cookie, "max-age=(\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value);
        maxAge.Should().BeInRange(1, 300);
        cookie.Should().NotContain("accessToken").And.NotContain("refreshToken");
    }

    [Fact]
    public async Task Callback_ForMfaEnabledUser_RedirectsToTheSecondStepWithoutTheTokenInTheUrl()
    {
        using var client = CreateClient(MfaOutcome());

        var response = await client.GetAsync(CallbackPath);

        var location = response.Headers.Location!.OriginalString;
        location.Should().Be("/sign-in?step=mfa");
        location.Should().NotContain("challenge-token");
        response.Headers.Location.OriginalString.Should().NotContain("error=");
    }

    [Fact]
    public async Task Callback_ForMfaEnabledUser_KeepsASafeReturnPath()
    {
        using var client = CreateClient(MfaOutcome(), storedReturnUrl: "/boards/42?tab=list");

        var response = await client.GetAsync(CallbackPath);

        response.Headers.Location!.OriginalString.Should().Be("/sign-in?step=mfa&returnTo=%2Fboards%2F42%3Ftab%3Dlist");
    }

    [Fact]
    public async Task Callback_ForMfaEnabledUser_OverHttps_MarksTheChallengeCookieSecure()
    {
        using var client = CreateClient(MfaOutcome(), https: true);

        var response = await client.GetAsync(CallbackPath);

        SetCookies(response).Should().ContainSingle().Which.Should().ContainEquivalentOf("secure");
    }

    [Fact]
    public async Task Callback_ForMfaEnabledUser_CapsTheCookieAtTheChallengeLifetime()
    {
        using var client = CreateClient(MfaOutcome(minutes: 1));

        var response = await client.GetAsync(CallbackPath);

        var cookie = SetCookies(response).Single();
        var maxAge = int.Parse(System.Text.RegularExpressions.Regex.Match(cookie, "max-age=(\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value);
        maxAge.Should().BeLessThanOrEqualTo(60);
    }

    [Fact]
    public async Task Callback_WithAnMfaResultThatHasNoChallenge_FailsClosedWithoutCookie()
    {
        using var client = CreateClient(Result<AuthResult>.Success(new AuthResult { MfaRequired = true }));

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.mfa-required");
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Callback_WithTokensMissingFromTheResult_FailsClosedWithoutCookie()
    {
        using var client = CreateClient(Result<AuthResult>.Success(new AuthResult()));

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Callback_ForUserWithoutMfa_SetsSessionCookies_AndRedirectsToTheSuccessUrl()
    {
        using var client = CreateClient(SessionOutcome());

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/auth/callback");
        var cookies = SetCookies(response);
        cookies.Should().Contain(cookie => cookie.StartsWith("accessToken="));
        cookies.Should().Contain(cookie => cookie.StartsWith("refreshToken="));
        cookies.Should().NotContain(cookie => cookie.StartsWith("mfaChallenge="));
    }

    [Fact]
    public async Task Callback_ForUserWithoutMfa_ReturnsToTheStoredInternalPath()
    {
        using var client = CreateClient(SessionOutcome(), storedReturnUrl: "/w/acme/boards/7");

        var response = await client.GetAsync(CallbackPath);

        response.Headers.Location!.OriginalString.Should().Be("/auth/callback?returnTo=%2Fw%2Facme%2Fboards%2F7");
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("//evil.com/phish")]
    [InlineData("/\\evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("evil.com")]
    [InlineData("/%2F%2Fevil.com")]
    [InlineData("/%2f/evil.com")]
    [InlineData("/%5Cevil.com")]
    [InlineData("/%252F%252Fevil.com")]
    [InlineData("/ok\r\nSet-Cookie: x=1")]
    [InlineData("/ok%0d%0aSet-Cookie:x=1")]
    [InlineData("/back\\slash")]
    public async Task Callback_DropsAnUnsafeStoredReturnPath(string unsafeReturnUrl)
    {
        using var session = CreateClient(SessionOutcome(), storedReturnUrl: unsafeReturnUrl);
        var sessionResponse = await session.GetAsync(CallbackPath);

        using var mfa = CreateClient(MfaOutcome(), storedReturnUrl: unsafeReturnUrl);
        var mfaResponse = await mfa.GetAsync(CallbackPath);

        sessionResponse.Headers.Location!.OriginalString.Should().Be("/auth/callback");
        mfaResponse.Headers.Location!.OriginalString.Should().Be("/sign-in?step=mfa");
    }

    [Fact]
    public async Task Callback_DropsAReturnPathLongerThanTheCap()
    {
        using var client = CreateClient(SessionOutcome(), storedReturnUrl: "/" + new string('a', 600));

        var response = await client.GetAsync(CallbackPath);

        response.Headers.Location!.OriginalString.Should().Be("/auth/callback");
    }
}
