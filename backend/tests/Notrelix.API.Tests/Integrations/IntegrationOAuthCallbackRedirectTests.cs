using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Application.Features.Integrations.Connections.Commands.CompleteIntegrationAuthorization;

namespace Notrelix.API.Tests.Integrations;

/// <summary>
/// Shared host for the provider callback: the state protector knows one state ("s") for one workspace, and the
/// authorization command is replaced so each test chooses the outcome and the callback's redirect is observed.
/// </summary>
public sealed class IntegrationCallbackHost : IDisposable
{
    public static readonly Guid WorkspaceId = Guid.Parse("3b1f6c2a-9d4e-4f7a-8b2c-1e5d6f7a8b9c");

    private readonly WebApplicationFactory<Program> _factory;

    public Result<IntegrationConnectionDto> Outcome { get; set; } = Result<IntegrationConnectionDto>.Failure("not set");

    /// <summary>How many times the authorization command reached its handler since the test began.</summary>
    public int HandlerCalls { get; set; }

    public IntegrationCallbackHost()
    {
        _factory = new NotrelixApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<CompleteIntegrationAuthorizationCommand, Result<IntegrationConnectionDto>>>();
            services.AddScoped<IRequestHandler<CompleteIntegrationAuthorizationCommand, Result<IntegrationConnectionDto>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<CompleteIntegrationAuthorizationCommand, Result<IntegrationConnectionDto>>>();
                handler.Setup(h => h.Handle(It.IsAny<CompleteIntegrationAuthorizationCommand>(), It.IsAny<CancellationToken>()))
                    .Callback(() => HandlerCalls++)
                    .ReturnsAsync(() => Outcome);
                return handler.Object;
            });

            services.RemoveAll<IIntegrationOAuthStateProtector>();
            services.AddSingleton(_ =>
            {
                var protector = new Mock<IIntegrationOAuthStateProtector>();
                protector.Setup(p => p.Unprotect("s"))
                    .Returns(new IntegrationOAuthState("google-calendar", WorkspaceId, Guid.Parse(TestAuthHandler.TestUserId), "https://api.example.test/cb"));
                protector.Setup(p => p.Unprotect(It.Is<string>(s => s != "s"))).Returns((IntegrationOAuthState?)null);
                return protector.Object;
            });
        }));
    }

    public HttpClient CreateSignedInClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }

    public void Dispose() => _factory.Dispose();
}

public sealed class IntegrationOAuthCallbackRedirectTests : IClassFixture<IntegrationCallbackHost>
{
    private const string ProviderCallback = "/api/v1/integrations/google-calendar/callback";

    private readonly IntegrationCallbackHost _host;

    public IntegrationOAuthCallbackRedirectTests(IntegrationCallbackHost host) => _host = host;

    private static string IntegrationsPage => $"/workspaces/{IntegrationCallbackHost.WorkspaceId}/integrations";

    private async Task<HttpResponseMessage> CallAsync(string query)
    {
        _host.HandlerCalls = 0;
        return await _host.CreateSignedInClient().GetAsync($"{ProviderCallback}?{query}");
    }

    [Fact]
    public async Task ProviderError_RedirectsWithErrorRefused_AndNeverRunsTheExchange()
    {
        var response = await CallAsync("state=s&error=access_denied");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().EndWith($"{IntegrationsPage}?error=refused",
            "a consent the member denied is explained, not reported as a failure");
        _host.HandlerCalls.Should().Be(0, "a refused consent carries no code to exchange");
    }

    [Fact]
    public async Task MissingCode_RedirectsWithErrorRefused_AndNeverRunsTheExchange()
    {
        var response = await CallAsync("state=s");

        response.Headers.Location!.OriginalString.Should().EndWith($"{IntegrationsPage}?error=refused");
        _host.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task FailedAuthorization_RedirectsWithErrorFailed_AndNamesNoProvider()
    {
        _host.Outcome = Result<IntegrationConnectionDto>.Failure("integrations.authorization.refused");

        var response = await CallAsync("code=good&state=s");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.OriginalString;
        location.Should().EndWith($"{IntegrationsPage}?error=failed");
        location.Should().NotContain("connected=");
        _host.HandlerCalls.Should().Be(1, "a code that reaches the handler is exchanged there");
    }

    [Fact]
    public async Task SuccessfulAuthorization_RedirectsWithConnectedProvider()
    {
        _host.Outcome = Result<IntegrationConnectionDto>.Success(new IntegrationConnectionDto(
            Guid.NewGuid(), "google-calendar", "active", Guid.Parse(TestAuthHandler.TestUserId),
            DateTimeOffset.UtcNow, null, null, null, null));

        var response = await CallAsync("code=good&state=s");

        response.Headers.Location!.OriginalString.Should().EndWith($"{IntegrationsPage}?connected=google-calendar");
    }

    [Fact]
    public async Task UnreadableState_RedirectsHome_WithoutReachingTheHandler()
    {
        var response = await CallAsync("code=good&state=tampered");

        var location = response.Headers.Location!.OriginalString;
        location.Should().EndWith("/", "an unknown state is sent to the app root, not to a workspace");
        location.Should().NotContain("error=").And.NotContain("connected=");
        _host.HandlerCalls.Should().Be(0);
    }
}
