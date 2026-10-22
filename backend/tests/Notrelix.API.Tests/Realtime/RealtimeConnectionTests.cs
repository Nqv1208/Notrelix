using System.Net.WebSockets;

using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

public sealed class RealtimeConnectionTests
{
    private static readonly Guid BoardId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly RealtimeScope WorkspaceScope = new(RealtimeScopeKind.Workspace, RealtimeGatewayHarness.WorkspaceId);
    private static readonly RealtimeScope BoardScope = new(RealtimeScopeKind.Board, BoardId);

    [Fact]
    public async Task Connection_announces_protocol_limits_first()
    {
        await using var harness = new RealtimeGatewayHarness();

        var ready = await harness.ReceiveAsync();

        ready.GetProperty("type").GetString().Should().Be("ready");
        ready.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        ready.GetProperty("maxSubscriptions").GetInt32().Should().Be(harness.Options.MaxSubscriptionsPerConnection);
    }

    [Fact]
    public async Task Authorized_subscription_receives_notify_only_events_after_the_ack()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");

        var ack = await harness.SubscribeAsync("ws-1", "workspace", WorkspaceScope.Id);
        ack.GetProperty("type").GetString().Should().Be("subscribed");
        ack.GetProperty("subscriptionId").GetString().Should().Be("ws-1");
        ack.GetProperty("streamKey").GetString().Should().Be(WorkspaceScope.StreamKey);

        await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, 1));
        var evt = await harness.ReceiveAsync("event");

        evt.GetProperty("subscriptionId").GetString().Should().Be("ws-1");
        evt.GetProperty("streamKey").GetString().Should().Be(WorkspaceScope.StreamKey);
        evt.GetProperty("streamVersion").GetInt64().Should().Be(1);
        evt.GetProperty("changeKind").GetString().Should().Be("CreateBoardItem");
        evt.GetProperty("workspaceId").GetGuid().Should().Be(RealtimeGatewayHarness.WorkspaceId);
        evt.GetProperty("resourceId").GetGuid().Should().Be(WorkspaceScope.Id);
        evt.TryGetProperty("occurredAt", out _).Should().BeTrue();
        evt.TryGetProperty("eventId", out _).Should().BeTrue();
        evt.TryGetProperty("correlationId", out _).Should().BeTrue();
        evt.TryGetProperty("payload", out _).Should().BeFalse("realtime only notifies and never carries resource data");
    }

    [Fact]
    public async Task Stream_versions_are_delivered_in_published_order_and_independent_per_stream()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);
        await harness.SubscribeAsync("bd", "board", BoardScope.Id);

        for (var version = 1; version <= 5; version++)
        {
            await harness.Source.Publish(BoardScope, RealtimeGatewayHarness.Notification(BoardScope, version));
            if (version == 2)
            {
                await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, 1));
            }
        }

        var received = new List<(string Sub, long Version)>();
        for (var i = 0; i < 6; i++)
        {
            var evt = await harness.ReceiveAsync("event");
            received.Add((evt.GetProperty("subscriptionId").GetString()!, evt.GetProperty("streamVersion").GetInt64()));
        }

        received.Where(r => r.Sub == "bd").Select(r => r.Version).Should().Equal(1, 2, 3, 4, 5);
        received.Where(r => r.Sub == "ws").Select(r => r.Version).Should().Equal(1);
    }

    [Fact]
    public async Task Non_member_subscription_is_denied_and_no_stream_is_opened()
    {
        var authorizer = new FakeAuthorizer { Default = RealtimeAuthorizationResult.Denied };
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");

        var response = await harness.SubscribeAsync("ws-1", "workspace", WorkspaceScope.Id);

        response.GetProperty("type").GetString().Should().Be("subscription-error");
        response.GetProperty("code").GetString().Should().Be("subscription_denied");
        harness.Source.OpenChannelCount.Should().Be(0, "a denied subscription must never attach to the stream");

        await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, 1));
        await harness.SendAsync(new { type = "ping", sentAt = "t" });
        var next = await harness.ReceiveAsync();
        next.GetProperty("type").GetString().Should().Be("pong", "no event may follow a denied subscription");
    }

    [Fact]
    public async Task Cross_tenant_board_is_denied_with_the_same_code_as_a_missing_board()
    {
        var authorizer = new FakeAuthorizer();
        authorizer.Set(BoardScope, RealtimeAuthorizationResult.Denied);
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");

        var response = await harness.SubscribeAsync("bd", "board", BoardScope.Id);

        response.GetProperty("code").GetString().Should().Be("subscription_denied");
    }

    [Fact]
    public async Task Authorization_system_failure_fails_closed()
    {
        var authorizer = new FakeAuthorizer { Default = RealtimeAuthorizationResult.Unavailable };
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");

        var response = await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        response.GetProperty("code").GetString().Should().Be("authorization_unavailable");
        harness.Source.OpenChannelCount.Should().Be(0);
    }

    [Fact]
    public async Task Unauthenticated_authorization_outcome_closes_with_4401()
    {
        var authorizer = new FakeAuthorizer { Default = RealtimeAuthorizationResult.Unauthenticated };
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");
        await harness.SendAsync(new { type = "subscribe", subscriptionId = "ws", scope = new { kind = "workspace", id = WorkspaceScope.Id } });

        var (status, _) = await harness.ReceiveUntilCloseAsync();

        ((int?)status).Should().Be(4401);
    }

    [Fact]
    public async Task Subscription_limit_is_enforced_per_connection()
    {
        var options = RealtimeGatewayHarness.FastOptions();
        options.MaxSubscriptionsPerConnection = 1;
        await using var harness = new RealtimeGatewayHarness(options);
        await harness.ReceiveAsync("ready");
        (await harness.SubscribeAsync("a", "workspace", WorkspaceScope.Id)).GetProperty("type").GetString().Should().Be("subscribed");

        var second = await harness.SubscribeAsync("b", "board", BoardScope.Id);

        second.GetProperty("code").GetString().Should().Be("subscription_limit");
        harness.Connection.SubscriptionCount.Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_subscriptions_are_idempotent_or_conflict()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("a", "workspace", WorkspaceScope.Id);

        (await harness.SubscribeAsync("a", "workspace", WorkspaceScope.Id)).GetProperty("type").GetString().Should().Be("subscribed");
        (await harness.SubscribeAsync("a", "board", BoardScope.Id)).GetProperty("code").GetString().Should().Be("subscription_conflict");
        (await harness.SubscribeAsync("b", "workspace", WorkspaceScope.Id)).GetProperty("code").GetString().Should().Be("subscription_conflict");
        harness.Connection.SubscriptionCount.Should().Be(1);
    }

    [Fact]
    public async Task Notifications_for_another_workspace_or_stream_are_never_forwarded()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        await harness.Source.Publish(WorkspaceScope,
            RealtimeGatewayHarness.Notification(WorkspaceScope, 1, workspaceId: Guid.NewGuid()));
        await harness.Source.Publish(WorkspaceScope,
            RealtimeGatewayHarness.Notification(WorkspaceScope, 2, streamKey: BoardScope.StreamKey));
        await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, 3));

        var evt = await harness.ReceiveAsync("event");
        evt.GetProperty("streamVersion").GetInt64().Should().Be(3, "mismatching tenant and stream notifications are dropped");
    }

    [Fact]
    public async Task Unsubscribe_stops_delivery_and_releases_the_stream()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        await harness.SendAsync(new { type = "unsubscribe", subscriptionId = "ws" });
        (await harness.ReceiveAsync("unsubscribed")).GetProperty("subscriptionId").GetString().Should().Be("ws");

        harness.Source.OpenChannelCount.Should().Be(0);
        await harness.SendAsync(new { type = "unsubscribe", subscriptionId = "ws" });
        (await harness.ReceiveAsync("subscription-error")).GetProperty("code").GetString().Should().Be("unknown_subscription");
    }

    [Fact]
    public async Task Revoked_membership_ends_the_subscription_at_the_next_reauthorization()
    {
        var authorizer = new FakeAuthorizer();
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        authorizer.Set(WorkspaceScope, RealtimeAuthorizationResult.Denied);

        var error = await harness.ReceiveAsync("subscription-error");
        error.GetProperty("code").GetString().Should().Be("subscription_revoked");
        error.GetProperty("subscriptionId").GetString().Should().Be("ws");
        harness.Source.OpenChannelCount.Should().Be(0);
        await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, 1));
        harness.Connection.SubscriptionCount.Should().Be(0);
    }

    [Fact]
    public async Task Reauthorization_failure_fails_closed_and_drops_the_subscription()
    {
        var authorizer = new FakeAuthorizer();
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        authorizer.Set(WorkspaceScope, RealtimeAuthorizationResult.Unavailable);

        var error = await harness.ReceiveAsync("subscription-error");
        error.GetProperty("code").GetString().Should().Be("authorization_unavailable");
        harness.Connection.SubscriptionCount.Should().Be(0);
    }

    [Fact]
    public async Task Revoked_credentials_close_the_connection_with_4401()
    {
        var credentials = new FakeCredentials();
        await using var harness = new RealtimeGatewayHarness(credentials: credentials);
        await harness.ReceiveAsync("ready");

        credentials.Valid = false;

        var (status, reason) = await harness.ReceiveUntilCloseAsync();
        ((int?)status).Should().Be(4401);
        reason.Should().Be("credentials_revoked");
    }

    [Fact]
    public async Task Expired_credentials_close_the_connection_with_4401()
    {
        await using var harness = new RealtimeGatewayHarness(credentialExpiresAt: DateTimeOffset.UtcNow.AddMilliseconds(300));
        await harness.ReceiveAsync("ready");

        var (status, reason) = await harness.ReceiveUntilCloseAsync();

        ((int?)status).Should().Be(4401);
        reason.Should().Be("credentials_expired");
    }

    [Fact]
    public async Task Server_sends_heartbeat_pings_and_answers_client_pings()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");

        (await harness.ReceiveAsync("ping")).TryGetProperty("sentAt", out _).Should().BeTrue();

        await harness.SendAsync(new { type = "ping", sentAt = "client-time" });
        (await harness.ReceiveAsync("pong")).GetProperty("sentAt").GetString().Should().Be("client-time");
    }

    [Fact]
    public async Task Silent_client_is_closed_with_heartbeat_timeout()
    {
        var options = RealtimeGatewayHarness.FastOptions();
        options.PingInterval = TimeSpan.FromMilliseconds(100);
        options.IdleTimeout = TimeSpan.FromMilliseconds(400);
        await using var harness = new RealtimeGatewayHarness(options);

        var (status, _) = await harness.ReceiveUntilCloseAsync();

        ((int?)status).Should().Be(4408);
    }

    [Fact]
    public async Task Malformed_message_is_rejected_without_closing()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");

        await harness.SendRawAsync("{not json");
        var error = await harness.ReceiveAsync("subscription-error");

        error.GetProperty("code").GetString().Should().Be("invalid_message");
        await harness.SendAsync(new { type = "ping" });
        (await harness.ReceiveAsync("pong")).GetProperty("type").GetString().Should().Be("pong");
    }

    [Fact]
    public async Task Oversized_message_closes_with_message_too_big()
    {
        var options = RealtimeGatewayHarness.FastOptions();
        options.MaxInboundMessageBytes = 256;
        await using var harness = new RealtimeGatewayHarness(options);
        await harness.ReceiveAsync("ready");

        await harness.SendRawAsync(new string('x', 1024));
        var (status, _) = await harness.ReceiveUntilCloseAsync();

        status.Should().Be(WebSocketCloseStatus.MessageTooBig);
    }

    [Fact]
    public async Task Flooding_inbound_messages_closes_with_policy_violation()
    {
        var options = RealtimeGatewayHarness.FastOptions();
        options.MaxInboundMessagesPerSecond = 3;
        await using var harness = new RealtimeGatewayHarness(options);
        await harness.ReceiveAsync("ready");

        for (var i = 0; i < 20; i++)
        {
            try
            {
                await harness.SendAsync(new { type = "pong" });
            }
            catch (WebSocketException)
            {
                break;
            }
        }

        var (status, _) = await harness.ReceiveUntilCloseAsync();
        status.Should().Be(WebSocketCloseStatus.PolicyViolation);
    }

    [Fact]
    public async Task Slow_consumer_is_closed_instead_of_buffering_without_bound()
    {
        var options = RealtimeGatewayHarness.FastOptions();
        options.SendQueueCapacity = 4;
        options.SendTimeout = TimeSpan.FromMilliseconds(500);
        await using var harness = new RealtimeGatewayHarness(options);
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);

        // The client stops reading; the in-memory pipe and the bounded queue fill up.
        for (var version = 1; version <= 4000; version++)
        {
            await harness.Source.Publish(WorkspaceScope, RealtimeGatewayHarness.Notification(WorkspaceScope, version));
        }

        await harness.Completed.WaitAsync(TimeSpan.FromSeconds(15));
        harness.Connection.SubscriptionCount.Should().Be(0);
        harness.Source.OpenChannelCount.Should().Be(0);
    }

    [Fact]
    public async Task Closing_the_connection_releases_every_stream()
    {
        var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ws", "workspace", WorkspaceScope.Id);
        await harness.SubscribeAsync("bd", "board", BoardScope.Id);
        harness.Source.OpenChannelCount.Should().Be(2);

        await harness.DisposeAsync();

        harness.Source.OpenChannelCount.Should().Be(0);
    }
}
