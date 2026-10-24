using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// After an OAuth sign-in the MFA challenge lives in an httpOnly cookie. The verify endpoint
/// accepts it when the body carries none (the body always wins), clears it on success and when
/// the challenge is gone, and answers a readable error when there is no challenge at all.
/// </summary>
public sealed class MfaVerifyHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public Result<AuthResult> Outcome { get; set; } = Result<AuthResult>.Failure("not set");

    public CompleteMfaChallengeCommand? Command { get; set; }

    public MfaVerifyHost()
    {
        // One host for the whole class (building a host is slow); each test sets the outcome first.
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteMfaChallengeCommand, Result<AuthResult>>>();
            services.AddScoped<IRequestHandler<CompleteMfaChallengeCommand, Result<AuthResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteMfaChallengeCommand, Result<AuthResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteMfaChallengeCommand>(), It.IsAny<CancellationToken>()))
                    .Callback<CompleteMfaChallengeCommand, CancellationToken>((command, _) => Command = command)
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });
        }));
    }

    public HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public void Dispose() => _factory.Dispose();
}

public class MfaChallengeCookieTests : IClassFixture<MfaVerifyHost>
{
    private const string VerifyPath = "/api/v1/auth/mfa/verify";

    private readonly MfaVerifyHost _host;

    public MfaChallengeCookieTests(MfaVerifyHost host) => _host = host;

    private sealed class Captured
    {
        private readonly MfaVerifyHost _host;

        public Captured(MfaVerifyHost host) => _host = host;

        public CompleteMfaChallengeCommand? Command => _host.Command;
    }

    private HttpClient CreateClient(Result<AuthResult> outcome, out Captured captured)
    {
        _host.Outcome = outcome;
        _host.Command = null;
        captured = new Captured(_host);
        return _host.CreateClient();
    }

    private static Result<AuthResult> Session() => Result<AuthResult>.Success(new AuthResult
    {
        AccessToken = "access",
        RefreshToken = "refresh",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
    });

    private static Result<AuthResult> InvalidChallenge() =>
        Result<AuthResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid);

    private static HttpRequestMessage Verify(string body, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, VerifyPath)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (cookie is not null)
            request.Headers.Add("Cookie", $"mfaChallenge={cookie}");
        return request;
    }

    private static string[] SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];

    private static bool ClearsChallengeCookie(string[] cookies) =>
        cookies.Any(cookie => cookie.StartsWith("mfaChallenge=;", StringComparison.Ordinal)
            && cookie.Contains("path=/api/v1/auth/mfa", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public async Task Verify_WithoutAChallengeInTheBody_UsesTheCookie_AndClearsItOnSuccess()
    {
        using var client = CreateClient(Session(), out var captured);

        var response = await client.SendAsync(Verify("{\"code\":\"123456\"}", "cookie-challenge"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Command!.ChallengeToken.Should().Be("cookie-challenge");
        captured.Command.Code.Should().Be("123456");
        var cookies = SetCookies(response);
        cookies.Should().Contain(cookie => cookie.StartsWith("accessToken="));
        cookies.Should().Contain(cookie => cookie.StartsWith("refreshToken="));
        ClearsChallengeCookie(cookies).Should().BeTrue("the challenge is consumed");
    }

    [Fact]
    public async Task Verify_WithAChallengeInTheBody_PrefersTheBodyOverTheCookie()
    {
        using var client = CreateClient(Session(), out var captured);

        var response = await client.SendAsync(
            Verify("{\"challengeToken\":\"body-challenge\",\"code\":\"123456\"}", "cookie-challenge"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Command!.ChallengeToken.Should().Be("body-challenge");
    }

    [Fact]
    public async Task Verify_WithABlankBodyChallenge_FallsBackToTheCookie()
    {
        using var client = CreateClient(Session(), out var captured);

        await client.SendAsync(Verify("{\"challengeToken\":\"  \",\"code\":\"123456\"}", "cookie-challenge"));

        captured.Command!.ChallengeToken.Should().Be("cookie-challenge");
    }

    [Fact]
    public async Task Verify_WhenTheCookieChallengeIsExhaustedOrExpired_ClearsTheCookie()
    {
        using var client = CreateClient(InvalidChallenge(), out var captured);

        var response = await client.SendAsync(Verify("{\"code\":\"123456\"}", "cookie-challenge"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorCodeAsync(response, "identity.mfa.challenge-invalid");
        ClearsChallengeCookie(SetCookies(response)).Should().BeTrue();
        SetCookies(response).Should().NotContain(cookie => cookie.StartsWith("accessToken="));
    }

    [Fact]
    public async Task Verify_WithAWrongCodeOnAStillValidChallenge_KeepsTheCookie()
    {
        using var client = CreateClient(
            Result<AuthResult>.Failure(new ApplicationError("identity.mfa.invalid-code", "Invalid MFA code.", ApplicationErrorType.Validation)),
            out _);

        var response = await client.SendAsync(Verify("{\"code\":\"000000\"}", "cookie-challenge"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorCodeAsync(response, "identity.mfa.invalid-code");
        SetCookies(response).Should().BeEmpty("the person can try the next code, up to the attempt limit");
    }

    [Fact]
    public async Task Verify_WithAnInvalidBodyChallenge_DoesNotTouchTheCookie()
    {
        using var client = CreateClient(InvalidChallenge(), out var captured);

        var response = await client.SendAsync(
            Verify("{\"challengeToken\":\"stale\",\"code\":\"123456\"}", "cookie-challenge"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        SetCookies(response).Should().BeEmpty();
    }

    [Fact]
    public async Task Verify_WithNeitherBodyNorCookie_AnswersAReadableChallengeError_WithoutCallingTheHandler()
    {
        using var client = CreateClient(Session(), out var captured);

        var response = await client.SendAsync(Verify("{\"code\":\"123456\"}", cookie: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorCodeAsync(response, "identity.mfa.challenge-invalid");
        captured.Command.Should().BeNull();
        SetCookies(response).Should().NotContain(cookie => cookie.StartsWith("accessToken="));
    }

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string expected)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errorCode").GetString().Should().Be(expected);
    }
}
