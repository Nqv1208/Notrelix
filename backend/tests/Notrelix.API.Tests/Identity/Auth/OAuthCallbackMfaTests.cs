using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// The OAuth login callback never turns a multi-factor challenge into a session: an MFA-enabled user
/// has no tokens yet, so the callback must set no cookie and send the browser to the failure page
/// with a stable error code.
/// </summary>
public class OAuthCallbackMfaTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public OAuthCallbackMfaTests(NotrelixApiFactory factory) => _factory = factory;

    private HttpClient CreateClient(Result<AuthResult> outcome)
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
            services.AddScoped<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteOAuthLoginCommand, Result<AuthResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteOAuthLoginCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(outcome);
                return handler.Object;
            });
        }));
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Callback_ForMfaEnabledUser_SetsNoCookie_AndRedirectsToFailureWithErrorCode()
    {
        using var client = CreateClient(Result<AuthResult>.Success(new AuthResult
        {
            MfaRequired = true,
            MfaChallengeToken = "challenge-token",
            MfaMethod = "AuthenticatorApp",
            MfaExpiresAt = DateTime.UtcNow.AddMinutes(5),
        }));

        var response = await client.GetAsync("/api/v1/auth/oauth/google/callback?code=c&state=s");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("error=identity.oauth.mfa-required");
        response.Headers.Location.OriginalString.Should().NotContain("challenge-token");
        response.Headers.Contains("Set-Cookie").Should().BeFalse("an MFA challenge is not a session");
    }

    [Fact]
    public async Task Callback_WithTokensMissingFromTheResult_FailsClosedWithoutCookie()
    {
        using var client = CreateClient(Result<AuthResult>.Success(new AuthResult()));

        var response = await client.GetAsync("/api/v1/auth/oauth/google/callback?code=c&state=s");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Callback_ForUserWithoutMfa_SetsSessionCookies()
    {
        using var client = CreateClient(Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        }));

        var response = await client.GetAsync("/api/v1/auth/oauth/google/callback?code=c&state=s");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.GetValues("Set-Cookie").Should().NotBeEmpty();
    }
}
