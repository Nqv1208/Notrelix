using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Notrelix.API.Realtime;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// HTTP edge of the realtime gateway through the real host pipeline (TestServer):
/// authentication, origin allow-list, upgrade requirement, per-user limit and a
/// full subscribe round trip with fake authorization.
/// </summary>
public sealed class RealtimeGatewayEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private const string Path = "/api/v1/realtime";
    private const string AllowedOrigin = "http://localhost:5173";

    private readonly NotrelixApiFactory _factory;

    public RealtimeGatewayEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> Host(
        FakeAuthorizer? authorizer = null,
        Dictionary<string, string?>? config = null)
        => _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(config ?? []));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRealtimeSubscriptionAuthorizer>();
                services.AddSingleton<IRealtimeSubscriptionAuthorizer>(authorizer ?? new FakeAuthorizer());
                services.RemoveAll<IRealtimeCredentialValidator>();
                services.AddSingleton<IRealtimeCredentialValidator>(new FakeCredentials());
            });
        });

    private static HttpRequestMessage Request(bool authenticated, string? origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Path);
        if (authenticated)
        {
            request.Headers.Add("X-Test-Auth", "true");
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return request;
    }

    [Fact]
    public async Task Unauthenticated_request_is_rejected_before_any_upgrade()
    {
        using var host = Host();
        using var client = host.CreateClient();

        var response = await client.SendAsync(Request(authenticated: false, AllowedOrigin));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unlisted_browser_origin_is_rejected_even_when_authenticated()
    {
        using var host = Host();
        using var client = host.CreateClient();

        var response = await client.SendAsync(Request(authenticated: true, "https://evil.example"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_plain_http_request_requires_an_upgrade()
    {
        using var host = Host();
        using var client = host.CreateClient();

        var response = await client.SendAsync(Request(authenticated: true, AllowedOrigin));

        response.StatusCode.Should().Be((HttpStatusCode)426);
    }

    [Fact]
    public async Task Bad_origin_cannot_complete_a_websocket_handshake()
    {
        using var host = Host();
        var wsClient = host.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request =>
        {
            request.Headers["X-Test-Auth"] = "true";
            request.Headers["Origin"] = "https://evil.example";
        };

        var act = () => wsClient.ConnectAsync(new Uri(host.Server.BaseAddress, Path), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>("the upgrade must be refused with 403");
    }

    [Fact]
    public async Task Authenticated_allowed_origin_connects_and_subscribes_through_the_host_pipeline()
    {
        var authorizer = new FakeAuthorizer();
        using var host = Host(authorizer);
        using var socket = await ConnectAsync(host);

        var ready = await ReceiveAsync(socket);
        ready.GetProperty("type").GetString().Should().Be("ready");

        var workspaceId = RealtimeGatewayHarness.WorkspaceId;
        await SendAsync(socket, new { type = "subscribe", subscriptionId = "s1", scope = new { kind = "workspace", id = workspaceId } });
        var ack = await ReceiveAsync(socket);
        ack.GetProperty("type").GetString().Should().Be("subscribed");

        var source = host.Services.GetRequiredService<IRealtimeNotificationSource>();
        source.Should().BeOfType<Notrelix.Infrastructure.Realtime.InMemoryRealtimeNotificationSource>();
        var scope = new RealtimeScope(RealtimeScopeKind.Workspace, workspaceId);
        await ((Notrelix.Infrastructure.Realtime.InMemoryRealtimeNotificationSource)source)
            .Publish(scope, RealtimeGatewayHarness.Notification(scope, 1));

        var evt = await ReceiveAsync(socket);
        evt.GetProperty("type").GetString().Should().Be("event");
        evt.GetProperty("streamVersion").GetInt64().Should().Be(1);
        authorizer.Calls.Should().Be(1);

        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
    }

    [Fact]
    public async Task Connections_per_user_are_limited()
    {
        using var host = Host(config: new() { ["Realtime:Gateway:MaxConnectionsPerUser"] = "1" });
        using var first = await ConnectAsync(host);
        await ReceiveAsync(first);

        var act = () => ConnectAsync(host);

        await act.Should().ThrowAsync<Exception>("the second connection must be refused with 429");
    }

    private static async Task<WebSocket> ConnectAsync(WebApplicationFactory<Program> host)
    {
        var wsClient = host.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request =>
        {
            request.Headers["X-Test-Auth"] = "true";
            request.Headers["Origin"] = AllowedOrigin;
        };
        return await wsClient.ConnectAsync(new Uri(host.Server.BaseAddress, Path), CancellationToken.None);
    }

    private static async Task SendAsync(WebSocket socket, object message)
        => await socket.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<JsonElement> ReceiveAsync(WebSocket socket)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[16 * 1024];
        var result = await socket.ReceiveAsync(buffer, cts.Token);
        using var document = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
        return document.RootElement.Clone();
    }
}
