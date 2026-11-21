using System.Net;
using System.Text;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Infrastructure.Identity.Sso;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Identity.Sso;

public class SsoCodeRedeemerTests
{
    private const string Issuer = "https://login.acme.example";
    private const string Secret = "client-secret-never-in-a-message-9f2";
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeOidcProvider _provider = new(Issuer, "client-1", Secret)
    {
        Email = "a@acme.com",
        Name = "Ann",
        Nonce = "nonce-1",
        CodeChallenge = Challenge("verifier-1"),
        Clock = () => Now,
    };

    private static string Challenge(string verifier) =>
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private SsoCodeRedeemer Redeemer(HttpMessageHandler? handler = null) =>
        new(new Factory(handler ?? _provider), new FakeOidcDiscoveryClient().Serve(Issuer), FakeDateTimeProvider.WithFixedTime(Now));

    private static SsoCodeRedemption Redemption(string code = "auth-code-1", string verifier = "verifier-1") =>
        new(Issuer, "client-1", Secret, code, verifier, "https://api.example/api/v1/auth/sso/callback", "nonce-1");

    [Fact]
    public async Task ARightCode_GivesTheClaimsOfTheVerifiedToken_AndSendsTheSecretOnlyAsBasicAuthorization()
    {
        var claims = await Redeemer().RedeemAsync(Redemption(), CancellationToken.None);

        claims.Should().Be(new SsoIdentityClaims("subject-1", "a@acme.com", true, "Ann"));
        var token = _provider.Requests.Single(r => r.Method == "POST");
        token.Url.Should().Be(Issuer + "/token");
        token.Authorization.Should().StartWith("Basic ");
        token.Body.Should().NotContain(Secret).And.Contain("code_verifier=verifier-1").And.Contain("grant_type=authorization_code");
        _provider.Requests.Where(r => r.Method == "GET").Should().ContainSingle(r => r.Url == Issuer + "/jwks");
    }

    [Fact]
    public async Task AWrongCodeVerifier_IsRefusedByTheProvider_AndTheFailureNeverCarriesTheSecretOrToken()
    {
        var failure = await FluentActions.Invoking(() => Redeemer().RedeemAsync(Redemption(verifier: "wrong"), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>();

        failure.Which.Message.Should().Be("token endpoint answered 400").And.NotContain(Secret);
    }

    [Fact]
    public async Task ATokenEndpointRefusal_IsAFailureWithoutDetail()
    {
        _provider.TokenStatus = HttpStatusCode.Unauthorized;

        (await FluentActions.Invoking(() => Redeemer().RedeemAsync(Redemption(), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>()).Which.Message.Should().Be("token endpoint answered 401");
    }

    [Fact]
    public async Task AResponseWithoutAnIdToken_IsRefused()
    {
        _provider.OmitIdToken = true;

        (await FluentActions.Invoking(() => Redeemer().RedeemAsync(Redemption(), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>()).Which.Message.Should().Be("token response has no id token");
    }

    [Fact]
    public async Task AnIdTokenWithTheWrongNonce_IsRefused()
    {
        _provider.NonceClaim = "someone-elses";

        (await FluentActions.Invoking(() => Redeemer().RedeemAsync(Redemption(), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>()).Which.Message.Should().Be("nonce mismatch");
    }

    [Fact]
    public async Task AnOversizedKeySet_IsRefused()
    {
        var oversized = new SizedHandler(_provider, jwksBytes: SsoCodeRedeemer.MaxResponseBytes + 10);

        (await FluentActions.Invoking(() => Redeemer(oversized).RedeemAsync(Redemption(), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>()).Which.Message.Should().Be("key set response too large");
    }

    [Fact]
    public async Task AProviderWhoseDiscoveryFails_IsRefused_AndNothingIsExchanged()
    {
        var redeemer = new SsoCodeRedeemer(
            new Factory(_provider), new FakeOidcDiscoveryClient().Refuse(Issuer, OidcDiscoveryFailure.AddressRefused), FakeDateTimeProvider.WithFixedTime(Now));

        (await FluentActions.Invoking(() => redeemer.RedeemAsync(Redemption(), CancellationToken.None))
            .Should().ThrowAsync<SsoRedemptionException>()).Which.Message.Should().Be("discovery failed (address-refused)");
        _provider.Requests.Should().BeEmpty();
    }

    [Fact]
    public void TheRedemptionRecordNeverPrintsTheSecret() => Redemption().ToString().Should().NotContain(Secret);

    private sealed class SizedHandler(HttpMessageHandler inner, int jwksBytes) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get) return await base.SendAsync(request, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string(' ', jwksBytes)) };
        }
    }
}
