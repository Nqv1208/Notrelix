using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>Records the commands the provider sign-in endpoints send, and answers each with a chosen outcome.</summary>
public sealed class OAuthBindingHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public StartOAuthLoginCommand? StartCommand { get; private set; }

    public CompleteOAuthLoginCommand? CompleteCommand { get; private set; }

    public Result<OAuthLoginStartResult> StartOutcome { get; set; } =
        Result<OAuthLoginStartResult>.Success(new OAuthLoginStartResult("https://accounts.google.com/o/oauth2/auth?x=1"));

    public Result<AuthResult> CompleteOutcome { get; set; } = Result<AuthResult>.Failure("not set");

    public OAuthBindingHost()
    {
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<StartOAuthLoginCommand, Result<OAuthLoginStartResult>>>();
            services.AddScoped<IRequestHandler<StartOAuthLoginCommand, Result<OAuthLoginStartResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<StartOAuthLoginCommand, Result<OAuthLoginStartResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<StartOAuthLoginCommand>(), It.IsAny<CancellationToken>()))
                    .Returns<StartOAuthLoginCommand, CancellationToken>((command, _) =>
                    {
                        StartCommand = command;
                        return Task.FromResult(StartOutcome);
                    });
                return handler.Object;
            });

            services.RemoveAll<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
            services.AddScoped<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteOAuthLoginCommand>(), It.IsAny<CancellationToken>()))
                    .Returns<CompleteOAuthLoginCommand, CancellationToken>((command, _) =>
                    {
                        CompleteCommand = command;
                        return Task.FromResult(CompleteOutcome);
                    });
                return handler.Object;
            });
        }));
    }

    public HttpClient CreateClient(bool https = false) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri(https ? "https://localhost" : "http://localhost"),
        });

    public void Dispose() => _factory.Dispose();
}

/// <summary>
/// A provider sign-in is tied to the browser that started it: the start sets a short-lived httpOnly cookie
/// scoped to the OAuth endpoints and the callback reads it, hands it to the command and clears it on every outcome.
/// </summary>
public class OAuthBrowserBindingEndpointTests : IClassFixture<OAuthBindingHost>
{
    private const string BindingCookie = "oauthBinding";

    private readonly OAuthBindingHost _host;

    public OAuthBrowserBindingEndpointTests(OAuthBindingHost host) => _host = host;

    private static string[] SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    private static string BindingSetCookie(HttpResponseMessage response) =>
        SetCookies(response).Single(c => c.StartsWith($"{BindingCookie}=", StringComparison.Ordinal));

    private static HttpRequestMessage Callback(string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/oauth/google/callback?code=c&state=s");
        if (cookie is not null) request.Headers.Add("Cookie", $"{BindingCookie}={cookie}");
        return request;
    }

    [Fact]
    public async Task Start_SetsAScopedHttpOnlyLaxCookie_AndKeepsOnlyItsValueForTheState()
    {
        using var client = _host.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/oauth/google/start");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().StartWith("https://accounts.google.com/");
        var cookie = BindingSetCookie(response);
        cookie.Should().Contain("path=/api/v1/auth/oauth");
        cookie.Should().ContainEquivalentOf("httponly");
        cookie.Should().ContainEquivalentOf("samesite=lax");
        cookie.Should().NotContainEquivalentOf("secure", "the request is plain HTTP");
        cookie.Should().ContainEquivalentOf("max-age=600");
        var value = cookie[(BindingCookie.Length + 1)..cookie.IndexOf(';')];
        value.Should().HaveLength(43);
        _host.StartCommand!.BrowserBinding.Should().Be(value);
    }

    [Fact]
    public async Task Start_OverHttps_MarksTheCookieSecure()
    {
        using var client = _host.CreateClient(https: true);

        var response = await client.GetAsync("/api/v1/auth/oauth/google/start");

        BindingSetCookie(response).Should().ContainEquivalentOf("secure");
    }

    [Fact]
    public async Task Start_ForAProviderThatIsNotAvailable_SetsNoCookie()
    {
        _host.StartOutcome = Result<OAuthLoginStartResult>.Failure(new ApplicationError(
            "identity.oauth.provider-unavailable", "off", ApplicationErrorType.NotFound));
        try
        {
            using var client = _host.CreateClient();

            var response = await client.GetAsync("/api/v1/auth/oauth/google/start");

            response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.provider-unavailable");
            SetCookies(response).Should().BeEmpty();
        }
        finally
        {
            _host.StartOutcome = Result<OAuthLoginStartResult>.Success(
                new OAuthLoginStartResult("https://accounts.google.com/o/oauth2/auth?x=1"));
        }
    }

    [Fact]
    public async Task Start_GivesEveryBrowserItsOwnValue()
    {
        using var client = _host.CreateClient();

        var first = BindingSetCookie(await client.GetAsync("/api/v1/auth/oauth/google/start"));
        var second = BindingSetCookie(await client.GetAsync("/api/v1/auth/oauth/google/start"));

        first[..first.IndexOf(';')].Should().NotBe(second[..second.IndexOf(';')]);
    }

    [Fact]
    public async Task Callback_HandsTheCookieValueToTheCommand_AndClearsTheCookie()
    {
        _host.CompleteOutcome = Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        using var client = _host.CreateClient();

        var response = await client.SendAsync(Callback("the-binding"));

        _host.CompleteCommand!.BrowserBinding.Should().Be("the-binding");
        response.Headers.Location!.OriginalString.Should().Be("/auth/callback");
        var cleared = BindingSetCookie(response);
        cleared.Should().Contain("path=/api/v1/auth/oauth");
        cleared.Should().ContainEquivalentOf("expires=Thu, 01 Jan 1970");
        SetCookies(response).Should().Contain(c => c.StartsWith("accessToken="));
    }

    [Fact]
    public async Task Callback_WithNoCookie_SendsNoBindingToTheCommand_AndRefusalClearsTheCookieToo()
    {
        _host.CompleteOutcome = Result<AuthResult>.Failure(new ApplicationError(
            "identity.oauth.state-invalid", "Invalid or expired OAuth state.", ApplicationErrorType.Authentication));
        using var client = _host.CreateClient();

        var response = await client.SendAsync(Callback(cookie: null));

        _host.CompleteCommand!.BrowserBinding.Should().BeNull();
        response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.state-invalid");
        BindingSetCookie(response).Should().ContainEquivalentOf("expires=Thu, 01 Jan 1970");
        SetCookies(response).Should().NotContain(c => c.StartsWith("accessToken=") || c.StartsWith("refreshToken="));
    }

    [Fact]
    public async Task Callback_ForAnUnknownProvider_StillClearsTheCookie()
    {
        using var client = _host.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/oauth/nowhere/callback?code=c&state=s");
        request.Headers.Add("Cookie", $"{BindingCookie}=the-binding");

        var response = await client.SendAsync(request);

        response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.provider-unavailable");
        BindingSetCookie(response).Should().ContainEquivalentOf("expires=Thu, 01 Jan 1970");
    }
}
