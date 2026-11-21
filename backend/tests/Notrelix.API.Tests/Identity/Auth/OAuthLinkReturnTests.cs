using System.Net;
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
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;
using Notrelix.Application.Features.Identity.OAuth.DTOs;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.API.Tests.Identity.Auth;

public sealed class OAuthLinkHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public Result<OAuthLinkResult> Outcome { get; set; } = Result<OAuthLinkResult>.Failure("not set");
    public string? StoredReturnUrl { get; set; }
    public OAuthFlowKind StoredFlow { get; set; } = OAuthFlowKind.Link;

    public OAuthLinkHost()
    {
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteOAuthLinkCommand, Result<OAuthLinkResult>>>();
            services.AddScoped<IRequestHandler<CompleteOAuthLinkCommand, Result<OAuthLinkResult>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteOAuthLinkCommand, Result<OAuthLinkResult>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteOAuthLinkCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });

            services.RemoveAll<IOptions<OAuthRedirectOptions>>();
            services.AddScoped<IOptions<OAuthRedirectOptions>>(_ =>
                Microsoft.Extensions.Options.Options.Create(new OAuthRedirectOptions()));

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

    public HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }

    public void Dispose() => _factory.Dispose();
}

/// <summary>
/// The browser comes back from the provider to the API host: it is sent on to the security page that started
/// the link, told in a marker how it ended (never who owns a colliding identity).
/// </summary>
public class OAuthLinkReturnTests : IClassFixture<OAuthLinkHost>
{
    private const string CallbackPath = "/api/v1/auth/oauth/google/link/callback?code=c&state=s";
    private const string Page = "/workspaces/w1/account/security";

    private readonly OAuthLinkHost _host;

    public OAuthLinkReturnTests(OAuthLinkHost host) => _host = host;

    private HttpClient Arrange(Result<OAuthLinkResult> outcome, string? returnUrl = Page, OAuthFlowKind flow = OAuthFlowKind.Link)
    {
        _host.Outcome = outcome;
        _host.StoredReturnUrl = returnUrl;
        _host.StoredFlow = flow;
        return _host.CreateClient();
    }

    private static Result<OAuthLinkResult> Linked(bool already) =>
        Result<OAuthLinkResult>.Success(new OAuthLinkResult(OAuthProvider.Google, "sub", already));

    [Fact]
    public async Task Callback_WhenLinked_ReturnsToThePageWithTheLinkedMarker()
    {
        using var client = Arrange(Linked(false));
        var response = await client.GetAsync(CallbackPath);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be($"{Page}?link=linked&provider=Google");
    }

    [Fact]
    public async Task Callback_WhenAlreadyLinkedToThePerson_SaysSo()
    {
        using var client = Arrange(Linked(true));
        var response = await client.GetAsync(CallbackPath);
        response.Headers.Location!.OriginalString.Should().Be($"{Page}?link=already&provider=Google");
    }

    [Fact]
    public async Task Callback_WhenAnotherPersonOwnsTheIdentity_SaysCollisionAndNamesNoOne()
    {
        using var client = Arrange(Result<OAuthLinkResult>.Failure(new ApplicationError(
            "identity.oauth.link-collision", "This provider identity is already linked to another account.", ApplicationErrorType.Conflict)));
        var response = await client.GetAsync(CallbackPath);
        response.Headers.Location!.OriginalString.Should().Be($"{Page}?link=collision&provider=Google");
    }

    [Fact]
    public async Task Callback_WhenAnythingElseFails_SaysFailed()
    {
        using var client = Arrange(Result<OAuthLinkResult>.Failure("Invalid or expired OAuth state."));
        var response = await client.GetAsync(CallbackPath);
        response.Headers.Location!.OriginalString.Should().Be($"{Page}?link=failed&provider=Google");
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example")]
    public async Task Callback_NeverReturnsToAnotherOrigin(string stored)
    {
        using var client = Arrange(Linked(false), stored);
        var response = await client.GetAsync(CallbackPath);
        response.Headers.Location!.OriginalString.Should().NotContain("evil.example");
    }

    [Fact]
    public async Task Callback_WithAStateOfAnotherFlow_ReturnsToNoPage()
    {
        using var client = Arrange(Linked(false), flow: OAuthFlowKind.StepUp);
        var response = await client.GetAsync(CallbackPath);
        response.Headers.Location!.OriginalString.Should().NotContain("account/security");
    }
}
