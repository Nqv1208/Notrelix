using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.API.Tests.Identity.Auth;

public sealed class OAuthStepUpHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public Result<OAuthStepUpResult> Outcome { get; set; } = Result<OAuthStepUpResult>.Failure("not set");

    public string? StoredReturnUrl { get; set; }

    public OAuthFlowKind StoredFlow { get; set; } = OAuthFlowKind.StepUp;

    public string FrontendSuccessUrl { get; set; } = "/auth/callback";

    public OAuthStepUpHost()
    {
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteOAuthStepUpCommand, Result<OAuthStepUpResult>>>();
            services.AddScoped<IRequestHandler<CompleteOAuthStepUpCommand, Result<OAuthStepUpResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteOAuthStepUpCommand, Result<OAuthStepUpResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteOAuthStepUpCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });

            services.RemoveAll<IOptions<OAuthRedirectOptions>>();
            services.AddScoped<IOptions<OAuthRedirectOptions>>(_ =>
                Microsoft.Extensions.Options.Options.Create(new OAuthRedirectOptions { FrontendSuccessUrl = FrontendSuccessUrl }));

            services.RemoveAll<IOAuthStateStore>();
            services.AddScoped<IOAuthStateStore>(_ =>
            {
                var store = new Mock<IOAuthStateStore>();
                store.Setup(s => s.PeekAsync("s", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new OAuthLoginState(
                        "s", "nonce", "verifier", OAuthProvider.Google, StoredReturnUrl, DateTimeOffset.UtcNow.AddMinutes(10),
                        StoredFlow, Guid.NewGuid(), Guid.NewGuid(), null));
                return store.Object;
            });
        }));
    }

    public HttpClient CreateClient() =>
        CreateClient(new Dictionary<string, string>());

    public HttpClient CreateClient(IReadOnlyDictionary<string, string> cookies)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        if (cookies.Count > 0)
            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));
        return client;
    }

    public void Dispose() => _factory.Dispose();
}

/// <summary>
/// A provider re-authentication hands its single-use proof(s) to the page in a short-lived httpOnly cookie that
/// only the step-up endpoints receive, never in a URL; one claim reads it and deletes it, and the browser returns
/// to the internal path given at start.
/// </summary>
public class OAuthStepUpProofHandoffTests : IClassFixture<OAuthStepUpHost>
{
    private const string CallbackPath = "/api/v1/auth/oauth/google/step-up/callback?code=c&state=s";
    private const string ClaimPath = "/api/v1/auth/security/step-up/claim-provider";
    private const string Page = "/workspaces/w1/account/security?stepUp=enroll";

    private readonly OAuthStepUpHost _host;

    public OAuthStepUpProofHandoffTests(OAuthStepUpHost host) => _host = host;

    private static Result<OAuthStepUpResult> Granted(bool spare = false) =>
        Result<OAuthStepUpResult>.Success(new OAuthStepUpResult(
            new StepUpProofResult("proofone", DateTimeOffset.UtcNow.AddMinutes(5)),
            spare ? new StepUpProofResult("prooftwo", DateTimeOffset.UtcNow.AddMinutes(5)) : null));

    private HttpClient Arrange(
        Result<OAuthStepUpResult> outcome, string? returnUrl = Page, OAuthFlowKind flow = OAuthFlowKind.StepUp,
        string frontendSuccessUrl = "/auth/callback")
    {
        _host.FrontendSuccessUrl = frontendSuccessUrl;
        _host.Outcome = outcome;
        _host.StoredReturnUrl = returnUrl;
        _host.StoredFlow = flow;
        return _host.CreateClient();
    }

    private static string[] SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    [Fact]
    public async Task Callback_HandsTheProofOverInAScopedHttpOnlyCookie_AndReturnsToThePage()
    {
        using var client = Arrange(Granted());

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(Page).And.NotContain("proofone");
        var cookie = SetCookies(response).Should().ContainSingle().Which;
        cookie.Should().StartWith("stepUpProof=proofone");
        cookie.Should().Contain("path=/api/v1/auth/security/step-up");
        cookie.Should().ContainEquivalentOf("httponly").And.ContainEquivalentOf("samesite=lax");
        var maxAge = int.Parse(System.Text.RegularExpressions.Regex.Match(cookie, "max-age=(\\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value);
        maxAge.Should().BeInRange(1, 300);
    }

    [Fact]
    public async Task Callback_WhenTheWebAppIsAnotherOrigin_ReturnsThereNotToTheApiHost()
    {
        using var client = Arrange(Granted(), frontendSuccessUrl: "https://app.example.test/auth/callback");
        var ok = await client.GetAsync(CallbackPath);
        ok.Headers.Location!.OriginalString.Should().Be($"https://app.example.test{Page}");

        using var failing = Arrange(Result<OAuthStepUpResult>.Failure("no"), frontendSuccessUrl: "https://app.example.test/auth/callback");
        var failed = await failing.GetAsync(CallbackPath);
        failed.Headers.Location!.OriginalString.Should().Be($"https://app.example.test{Page}&stepUpResult=failed");
    }

    [Fact]
    public async Task Callback_ForEnrollment_CarriesTheSpareProofToo()
    {
        using var client = Arrange(Granted(spare: true));

        var response = await client.GetAsync(CallbackPath);

        SetCookies(response).Should().ContainSingle().Which.Should().StartWith("stepUpProof=proofone.prooftwo");
    }

    [Fact]
    public async Task Callback_Failure_SetsNoCookie_AndReturnsToThePageWithTheFailureMarker()
    {
        using var client = Arrange(Result<OAuthStepUpResult>.Failure("Unable to verify identity with the OAuth provider."));

        var response = await client.GetAsync(CallbackPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be($"{Page}&stepUpResult=failed");
        SetCookies(response).Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example")]
    public async Task Callback_NeverReturnsToAnotherOrigin(string stored)
    {
        using var client = Arrange(Granted(), stored);

        var response = await client.GetAsync(CallbackPath);

        response.Headers.Location!.OriginalString.Should().NotContain("evil.example");
    }

    [Fact]
    public async Task Callback_WithAStateOfAnotherFlow_ReturnsToNoPage()
    {
        using var client = Arrange(Granted(), flow: OAuthFlowKind.Login);

        var response = await client.GetAsync(CallbackPath);

        response.Headers.Location!.OriginalString.Should().NotContain("account/security");
    }

    [Fact]
    public async Task Claim_ReturnsTheProofOnce_AndDeletesTheCookie()
    {
        using var client = _host.CreateClient(new Dictionary<string, string> { ["stepUpProof"] = "proofone.prooftwo" });

        var response = await client.PostAsync(ClaimPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProviderStepUpClaimResult>();
        body.Should().Be(new ProviderStepUpClaimResult("proofone", "prooftwo"));
        SetCookies(response).Should().ContainSingle().Which.Should().StartWith("stepUpProof=;").And.Contain("path=/api/v1/auth/security/step-up");
    }

    [Fact]
    public async Task Claim_WithoutTheCookie_IsRefusedWith400()
    {
        using var client = _host.CreateClient();

        var response = await client.PostAsync(ClaimPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("identity.security.step-up-invalid");
    }

    [Fact]
    public async Task Claim_WithoutASession_IsUnauthorized()
    {
        using var factory = new NotrelixApiFactory();

        var response = await factory.CreateClient().PostAsync(ClaimPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
