using System.Net;
using System.Text;
using Notrelix.Application.Common.Oidc;
using Notrelix.Infrastructure.Oidc;

namespace Notrelix.Infrastructure.Tests.Oidc;

public sealed class OidcDiscoveryClientTests
{
    private const string Issuer = "https://login.acme.example";

    private static string Doc(string issuer = Issuer, string? token = null, string? jwks = null, bool authorization = true) =>
        "{" + $"\"issuer\":\"{issuer}\"," +
        (authorization ? $"\"authorization_endpoint\":\"{issuer}/authorize\"," : "") +
        $"\"token_endpoint\":\"{token ?? issuer + "/token"}\",\"jwks_uri\":\"{jwks ?? issuer + "/jwks"}\"" + "}";

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(answer(request));
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (OidcDiscoveryClient Client, Stub Handler) Client(Func<HttpRequestMessage, HttpResponseMessage> answer, bool allowPrivate = false)
    {
        var stub = new Stub(answer);
        return (new OidcDiscoveryClient(new Factory(stub), Microsoft.Extensions.Options.Options.Create(new OidcOutboundOptions { AllowPrivateTargets = allowPrivate })), stub);
    }

    [Fact]
    public async Task AGoodDocument_ForTheSameIssuer_IsReturned_FromTheWellKnownAddress()
    {
        var (client, stub) = Client(_ => Json(Doc()));

        var result = await client.DiscoverAsync(Issuer, default);

        result.Succeeded.Should().BeTrue();
        result.Document.Should().BeEquivalentTo(new OidcDiscoveryDocument(Issuer, Issuer + "/authorize", Issuer + "/token", Issuer + "/jwks"));
        stub.Requests.Should().ContainSingle().Which.ToString().Should().Be(Issuer + "/.well-known/openid-configuration");
    }

    [Fact]
    public async Task ATrailingSlashIssuer_StillReadsTheWellKnownAddressOnce()
    {
        var (client, stub) = Client(_ => Json(Doc(Issuer + "/")));

        var result = await client.DiscoverAsync(Issuer + "/", default);

        result.Succeeded.Should().BeTrue();
        stub.Requests.Single().ToString().Should().Be(Issuer + "/.well-known/openid-configuration");
    }

    [Fact]
    public async Task ADocumentThatSpeaksForAnotherIssuer_IsRefused()
    {
        var (client, _) = Client(_ => Json(Doc("https://evil.example")));

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.IssuerMismatch);
    }

    [Fact]
    public async Task AnHttpIssuer_IsRefused_WithoutAnyRequest()
    {
        var (client, stub) = Client(_ => Json(Doc()));

        (await client.DiscoverAsync("http://login.acme.example", default)).Failure.Should().Be(OidcDiscoveryFailure.NotHttps);
        stub.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://10.0.0.5")]
    [InlineData("https://192.168.1.10")]
    [InlineData("https://172.16.0.1")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://169.254.169.254")]
    [InlineData("https://[::1]")]
    [InlineData("https://[fe80::1]")]
    [InlineData("https://[::ffff:10.0.0.1]")]
    [InlineData("https://100.64.0.1")]
    [InlineData("https://localhost")]
    [InlineData("https://LOCALHOST")]
    public async Task AnIssuerOnAnInternalAddress_IsRefused_WithoutAnyRequest(string issuer)
    {
        var (client, stub) = Client(_ => Json(Doc(issuer)));

        (await client.DiscoverAsync(issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.AddressRefused);
        stub.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("https://user:pw@login.acme.example")]
    [InlineData("https://login.acme.example/?a=1")]
    public async Task AMalformedIssuer_IsRefused(string issuer)
    {
        var (client, stub) = Client(_ => Json(Doc()));

        (await client.DiscoverAsync(issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.IssuerInvalid);
        stub.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("http://login.acme.example/token", null)]
    [InlineData(null, "https://169.254.169.254/jwks")]
    [InlineData("https://10.1.2.3/token", null)]
    [InlineData(null, "http://login.acme.example/jwks")]
    public async Task ADocumentWithAnEndpointThatIsNotHttpsOrIsInternal_IsRefused(string? token, string? jwks)
    {
        var (client, _) = Client(_ => Json(Doc(token: token, jwks: jwks)));

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.EndpointInvalid);
    }

    [Fact]
    public async Task ADocumentWithoutTheAuthorizationEndpoint_IsRefused()
    {
        var (client, _) = Client(_ => Json(Doc(authorization: false)));

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.DocumentInvalid);
    }

    [Theory]
    [InlineData("<html>login</html>")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"issuer\":5}")]
    public async Task ADocumentThatIsNotADiscoveryObject_IsRefused(string body)
    {
        var (client, _) = Client(_ => Json(body));

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.DocumentInvalid);
    }

    [Fact]
    public async Task ARedirect_IsNotFollowed()
    {
        var (client, stub) = Client(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://169.254.169.254/latest/meta-data");
            return response;
        });

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.Redirected);
        stub.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task ANon2xxAnswer_IsRefused()
    {
        var (client, _) = Client(_ => Json("{}", HttpStatusCode.InternalServerError));

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.BadStatus);
    }

    [Fact]
    public async Task ADocumentOverTheSizeCap_IsRefused_EvenWithoutAContentLength()
    {
        var (client, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(new string(' ', OidcDiscoveryClient.MaxDocumentBytes + 10)))),
        });

        (await client.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.TooLarge);
    }

    [Fact]
    public async Task AnUnreachableProvider_AndATimeout_AreStableReasons_NotExceptions()
    {
        var (down, _) = Client(_ => throw new HttpRequestException("connection refused"));
        var (slow, _) = Client(_ => throw new TaskCanceledException("timed out"));

        (await down.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.Unreachable);
        (await slow.DiscoverAsync(Issuer, default)).Failure.Should().Be(OidcDiscoveryFailure.Timeout);
    }

    [Fact]
    public async Task TheConnectStep_RefusesAnInternalAddress_EvenWhenTheNameLooksOrdinary()
    {
        // The guarded handler itself: whatever the URL says, the connection only goes to checked addresses.
        using var handler = OidcGuardedHttp.CreateHandler(allowPrivate: false);
        using var http = new HttpClient(handler);

        var act = () => http.GetAsync("https://127.0.0.1:9/.well-known/openid-configuration");

        var thrown = await act.Should().ThrowAsync<HttpRequestException>();
        thrown.Which.InnerException.Should().BeOfType<Notrelix.Infrastructure.Integrations.Connections.BlockedWebhookAddressException>();
    }
}
