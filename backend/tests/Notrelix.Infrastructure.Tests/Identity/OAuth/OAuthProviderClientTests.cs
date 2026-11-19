using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Infrastructure.Identity.OAuth;

namespace Notrelix.Infrastructure.Tests.Identity.OAuth;

public class OAuthProviderClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    /// <summary>RFC 6749 4.1.3: the token request names its grant. Google answers 400 without it.</summary>
    [Fact]
    public async Task TheCodeExchange_NamesTheAuthorizationCodeGrant()
    {
        var handler = new CapturingHandler();
        var options = Microsoft.Extensions.Options.Options.Create(new OAuthOptions
        {
            Providers =
            {
                ["Google"] = new OAuthProviderConfig
                {
                    Enabled = true,
                    ClientId = "client-1",
                    ClientSecret = "secret-1",
                    TokenEndpoint = "http://provider.test/token",
                    Issuer = "https://accounts.google.com",
                    RedirectUri = "http://app.test/callback",
                    Scopes = ["openid"],
                },
            },
        });
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);
        var client = new OAuthProviderClient(options, new HttpClient(handler), clock.Object);

        await client
            .Invoking(c => c.RedeemCodeAsync(
                OAuthProvider.Google,
                new OAuthCodeRedemptionRequest("code-1", "verifier-1", "nonce-1", "http://app.test/callback"),
                CancellationToken.None))
            .Should().ThrowAsync<Exception>();

        handler.Body.Should().Contain("grant_type=authorization_code");
    }

    private static OAuthProviderClient ClientFor(string provider)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new OAuthOptions
        {
            Providers =
            {
                [provider] = new OAuthProviderConfig
                {
                    Enabled = true,
                    ClientId = "client-1",
                    ClientSecret = "secret-1",
                    AuthorizationEndpoint = "http://provider.test/authorize",
                    RedirectUri = "http://app.test/callback",
                    Scopes = ["openid"],
                },
            },
        });
        return new OAuthProviderClient(options, new HttpClient(new CapturingHandler()), new Mock<IDateTimeProvider>().Object);
    }

    private static async Task<IReadOnlyDictionary<string, string>> AuthorizationQuery(string provider, bool fresh)
    {
        var url = (await ClientFor(provider).BuildAuthorizationUrlAsync(
            Enum.Parse<OAuthProvider>(provider),
            new OAuthAuthorizationRequest("http://app.test/callback", "state-1", "nonce-1", "challenge-1", "S256", fresh),
            CancellationToken.None)).AuthorizationUrl;
        return new Uri(url).Query.TrimStart('?').Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    public async Task AStepUp_AsksTheProviderToSignThePersonInAgain(string provider)
    {
        var query = await AuthorizationQuery(provider, fresh: true);

        query["prompt"].Should().Be("login");
        query["max_age"].Should().Be("0");
    }

    [Fact]
    public async Task AnOrdinarySignIn_KeepsItsPrompt_AndAsksForNoFreshLogin()
    {
        var query = await AuthorizationQuery("Google", fresh: false);

        query["prompt"].Should().Be("consent");
        query.Should().NotContainKey("max_age");
    }

    [Fact]
    public async Task AnOrdinaryMicrosoftSignIn_AsksForNoPrompt()
    {
        var query = await AuthorizationQuery("Microsoft", fresh: false);

        query.Should().NotContainKey("prompt");
        query.Should().NotContainKey("max_age");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            // Stops the exchange here: only the request is under test.
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }
    }
}
