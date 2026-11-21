using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Application.Features.Identity.Sso.Commands.CompleteSsoSignIn;
using Notrelix.Application.Features.Identity.Sso.Commands.StartSsoSignIn;

namespace Notrelix.API.Tests.Identity.Auth;

public sealed class SsoCallbackHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public Result<SsoSignInResult> Outcome { get; set; } = Result<SsoSignInResult>.Failure("not set");

    public StartSsoSignInCommand? LastStart { get; private set; }

    public CompleteSsoSignInCommand? LastComplete { get; private set; }

    public Result<SsoStartResult> StartOutcome { get; set; } = Result<SsoStartResult>.Failure("not set");

    public SsoCallbackHost()
    {
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteSsoSignInCommand, Result<SsoSignInResult>>>();
            services.AddScoped<IRequestHandler<CompleteSsoSignInCommand, Result<SsoSignInResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteSsoSignInCommand, Result<SsoSignInResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteSsoSignInCommand>(), It.IsAny<CancellationToken>()))
                    .Callback<CompleteSsoSignInCommand, CancellationToken>((c, _) => LastComplete = c)
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });
            services.RemoveAll<IRequestHandler<StartSsoSignInCommand, Result<SsoStartResult>>>();
            services.AddScoped<IRequestHandler<StartSsoSignInCommand, Result<SsoStartResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<StartSsoSignInCommand, Result<SsoStartResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<StartSsoSignInCommand>(), It.IsAny<CancellationToken>()))
                    .Callback<StartSsoSignInCommand, CancellationToken>((c, _) => LastStart = c)
                    .ReturnsAsync(() => StartOutcome);
                return handler.Object;
            });
        }));
    }

    public HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://api.example") });

    public void Dispose() => _factory.Dispose();
}

/// <summary>The company sign-in endpoints: the callback ends in a redirect with cookies or one normalized error code, the start answers the provider address from the one callback address.</summary>
public class SsoEndpointTests : IClassFixture<SsoCallbackHost>
{
    private const string Callback = "/api/v1/auth/sso/callback?code=c&state=s";

    private readonly SsoCallbackHost _host;

    public SsoEndpointTests(SsoCallbackHost host) => _host = host;

    private static string[] AllSetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    /// <summary>Every cookie except the binding cookie, which the callback clears on every outcome.</summary>
    private static string[] SetCookies(HttpResponseMessage response) =>
        AllSetCookies(response).Where(c => !c.StartsWith("ssoBinding=", StringComparison.Ordinal)).ToArray();

    private static Result<SsoSignInResult> Failure(string code) =>
        Result<SsoSignInResult>.Failure(new ApplicationError(code, "details that must never reach the browser", ApplicationErrorType.Authentication));

    private async Task<HttpResponseMessage> CallbackAsync(Result<SsoSignInResult> outcome, string? bindingCookie = null)
    {
        _host.Outcome = outcome;
        using var client = _host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Callback);
        if (bindingCookie is not null) request.Headers.Add("Cookie", $"ssoBinding={bindingCookie}");
        return await client.SendAsync(request);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheCallback_PassesTheBindingCookieToTheCommand_AndAlwaysClearsIt(bool succeeds)
    {
        var outcome = succeeds
            ? Result<SsoSignInResult>.Success(new SsoSignInResult(new AuthResult { AccessToken = "a", RefreshToken = "r", ExpiresAt = DateTime.UtcNow.AddHours(1) }, null))
            : Failure("identity.sso.state-invalid");

        var response = await CallbackAsync(outcome, "the-raw-binding");

        _host.LastComplete!.BrowserBinding.Should().Be("the-raw-binding");
        var cleared = AllSetCookies(response).Single(c => c.StartsWith("ssoBinding=;", StringComparison.Ordinal));
        cleared.Should().ContainEquivalentOf("path=/api/v1/auth/sso/callback").And.ContainEquivalentOf("httponly").And.ContainEquivalentOf("samesite=lax");
    }

    [Fact]
    public async Task TheCallbackWithoutTheCookie_PassesNoBindingToTheCommand()
    {
        await CallbackAsync(Failure("identity.sso.state-invalid"));

        _host.LastComplete!.BrowserBinding.Should().BeNull();
    }

    [Fact]
    public async Task ASession_SetsTheCookies_AndRedirectsToTheSuccessPageWithTheSafeReturnPath()
    {
        var response = await CallbackAsync(Result<SsoSignInResult>.Success(new SsoSignInResult(
            new AuthResult { AccessToken = "access", RefreshToken = "refresh", ExpiresAt = DateTime.UtcNow.AddHours(1) }, "/boards/1")));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/auth/callback?returnTo=%2Fboards%2F1");
        SetCookies(response).Should().Contain(c => c.StartsWith("accessToken=access", StringComparison.Ordinal))
            .And.Contain(c => c.StartsWith("refreshToken=refresh", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("identity.sso.state-invalid")]
    [InlineData("identity.sso.denied")]
    [InlineData("identity.sso.failed")]
    [InlineData("identity.sso.email-unverified")]
    [InlineData("identity.sso.domain-mismatch")]
    [InlineData("identity.sso.identity-conflict")]
    [InlineData("identity.sso.not-provisioned")]
    [InlineData("identity.sso.unavailable")]
    public async Task EveryFailure_RedirectsToTheSignInPageWithThatOneCode_AndSetsNoCookie(string code)
    {
        var response = await CallbackAsync(Failure(code));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be($"/sign-in?error={code}");
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task AnythingElse_ReadsAsTheGenericFailure_NeverTheDetail()
    {
        var response = await CallbackAsync(Failure("db.broke: connection string password=hunter2"));

        response.Headers.Location!.OriginalString.Should().Be("/sign-in?error=identity.sso.failed");
    }

    [Fact]
    public async Task APersonWithASecondFactor_GetsTheChallengeCookieAndTheSecondStep_AndNoSession()
    {
        var response = await CallbackAsync(Result<SsoSignInResult>.Success(new SsoSignInResult(new AuthResult
        {
            MfaRequired = true,
            MfaChallengeToken = "challenge-token",
            MfaMethod = "AuthenticatorApp",
            MfaExpiresAt = DateTime.UtcNow.AddMinutes(5),
        }, null)));

        response.Headers.Location!.OriginalString.Should().Be("/sign-in?step=mfa");
        SetCookies(response).Should().ContainSingle().Which.Should().StartWith("mfaChallenge=challenge-token");
    }

    [Fact]
    public async Task AnAccountThatRequiresMfa_SendsAPersonWithoutAFactorToEnrol()
    {
        var response = await CallbackAsync(Result<SsoSignInResult>.Success(new SsoSignInResult(new AuthResult
        {
            MfaEnrollmentRequired = true,
            MfaEnrollmentToken = "enrol-token",
            MfaExpiresAt = DateTime.UtcNow.AddMinutes(5),
        }, null)));

        response.Headers.Location!.OriginalString.Should().Be("/sign-in?step=mfa&enroll=1");
        SetCookies(response).Should().ContainSingle().Which.Should().StartWith("mfaChallenge=enrol-token");
    }

    [Fact]
    public async Task ASuccessWithoutTokens_NeverPassesAsASession()
    {
        var response = await CallbackAsync(Result<SsoSignInResult>.Success(new SsoSignInResult(new AuthResult(), null)));

        response.Headers.Location!.OriginalString.Should().Be("/sign-in?error=identity.oauth.mfa-required");
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Start_AnswersTheAuthorizationAddress_AndHandsTheOneCallbackAddressToTheCommand()
    {
        _host.StartOutcome = Result<SsoStartResult>.Success(new SsoStartResult("https://login.acme.example/authorize?x=1"));
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sso/start", new { email = "ann@acme.com", returnUrl = "/boards/1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("authorizationUrl").GetString().Should().Be("https://login.acme.example/authorize?x=1");
        _host.LastStart!.RedirectUri.Should().Be("https://api.example/api/v1/auth/sso/callback");
        _host.LastStart.ReturnUrl.Should().Be("/boards/1");

        var cookie = AllSetCookies(response).Single(c => c.StartsWith("ssoBinding=", StringComparison.Ordinal));
        cookie.Should().ContainEquivalentOf("path=/api/v1/auth/sso/callback").And.ContainEquivalentOf("httponly")
            .And.ContainEquivalentOf("samesite=lax").And.ContainEquivalentOf("secure").And.ContainEquivalentOf("max-age=600");
        var raw = cookie["ssoBinding=".Length..].Split(';')[0];
        raw.Should().Be(_host.LastStart.BrowserBinding, "the cookie holds exactly what the command hashes");
        raw.Length.Should().BeGreaterThanOrEqualTo(43);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(raw);
    }

    [Fact]
    public async Task Start_RefusesAnUnroutableEmail_With404AndTheStableCode()
    {
        _host.StartOutcome = Result<SsoStartResult>.Failure(new ApplicationError(SsoErrorCodes.Unavailable, "n/a", ApplicationErrorType.NotFound));
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sso/start", new { email = "ann@nowhere.example" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errorCode").GetString().Should().Be("identity.sso.unavailable");
        AllSetCookies(response).Should().BeEmpty("a refused start sets no binding");
    }

    [Fact]
    public async Task APasswordSignInRefusedBecauseTheAccountRequiresSso_Answers403WithTheStableCode()
    {
        using var factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<Notrelix.Application.Features.Identity.Auth.Commands.Login.LoginCommand, Result<AuthResult>>>();
            services.AddScoped<IRequestHandler<Notrelix.Application.Features.Identity.Auth.Commands.Login.LoginCommand, Result<AuthResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<Notrelix.Application.Features.Identity.Auth.Commands.Login.LoginCommand, Result<AuthResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<Notrelix.Application.Features.Identity.Auth.Commands.Login.LoginCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthResult>.Failure(new ApplicationError(SsoErrorCodes.SsoRequired, "n/a", ApplicationErrorType.Forbidden)));
                return handler.Object;
            });
        }));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "ann@acme.com", password = string.Concat((char)65, (char)97, (char)49, (char)33, Guid.NewGuid().ToString("N")) });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errorCode").GetString().Should().Be("identity.auth.sso-required");
    }
}
