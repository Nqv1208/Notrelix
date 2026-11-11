using System.Text;
using System.Text.Json;
using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// The Chat scopes on the gateway (SYS-ADR-001 protocol v1.1): a channel stream carries the compact message frames and is
/// denied identically to a missing channel; a removed member's subscription closes on the access signal; the per-user
/// stream reaches only the person it names; no frame is larger than 4096 bytes.
/// </summary>
public sealed class ChatRealtimeGatewayTests
{
    private static readonly Guid SpaceId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly RealtimeScope ChannelScope = new(RealtimeScopeKind.Channel, SpaceId);

    private static JsonElement MessagePayload(long sequence, string content) =>
        JsonDocument.Parse($"{{\"id\":\"{Guid.NewGuid()}\",\"sequence\":{sequence},\"content\":\"{content}\",\"contentOmitted\":false,\"deleted\":false}}").RootElement.Clone();

    private static RealtimeNotification MessageFrame(RealtimeScope scope, long version, JsonElement payload, string eventType = "chat.message.created") =>
        RealtimeGatewayHarness.Notification(scope, version) with
        {
            EventType = eventType,
            ChangeKind = eventType,
            ResourceKind = "Channel",
            PayloadContract = "chat.message.v1",
            Payload = payload,
        };

    [Fact]
    public async Task A_member_subscribed_to_a_channel_receives_the_message_frame_with_its_payload()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");

        var ack = await harness.SubscribeAsync("ch", "channel", SpaceId);
        ack.GetProperty("scope").GetProperty("kind").GetString().Should().Be("channel");
        ack.GetProperty("streamKey").GetString().Should().Be($"chat:Channel:{SpaceId:N}");

        await harness.Source.Publish(ChannelScope, MessageFrame(ChannelScope, 4, MessagePayload(12, "hi there")));
        var evt = await harness.ReceiveAsync("event");

        evt.GetProperty("subscriptionId").GetString().Should().Be("ch");
        evt.GetProperty("eventType").GetString().Should().Be("chat.message.created");
        evt.GetProperty("changeKind").GetString().Should().Be("chat.message.created");
        evt.GetProperty("streamKey").GetString().Should().Be($"chat:Channel:{SpaceId:N}");
        evt.GetProperty("streamVersion").GetInt64().Should().Be(4);
        evt.GetProperty("payloadContract").GetString().Should().Be("chat.message.v1");
        var payload = evt.GetProperty("payload");
        payload.GetProperty("sequence").GetInt64().Should().Be(12);
        payload.GetProperty("content").GetString().Should().Be("hi there");
        evt.TryGetProperty("payloadOmitted", out _).Should().BeFalse();
    }

    [Fact]
    public async Task A_non_member_and_a_missing_channel_get_the_same_denial()
    {
        var missingScope = new RealtimeScope(RealtimeScopeKind.Channel, Guid.NewGuid());
        var authorizer = new FakeAuthorizer();
        authorizer.Set(ChannelScope, RealtimeAuthorizationResult.Denied);
        authorizer.Set(missingScope, RealtimeAuthorizationResult.Denied);
        await using var harness = new RealtimeGatewayHarness(authorizer: authorizer);
        await harness.ReceiveAsync("ready");

        await harness.SendAsync(new { type = "subscribe", subscriptionId = "forbidden", scope = new { kind = "channel", id = SpaceId } });
        var forbidden = await harness.ReceiveAsync("subscription-error");
        await harness.SendAsync(new { type = "subscribe", subscriptionId = "missing", scope = new { kind = "channel", id = missingScope.Id } });
        var missing = await harness.ReceiveAsync("subscription-error");

        forbidden.GetProperty("code").GetString().Should().Be("subscription_denied");
        missing.GetProperty("code").GetString().Should().Be(forbidden.GetProperty("code").GetString());
        harness.Connection.SubscriptionCount.Should().Be(0);
    }

    [Fact]
    public async Task A_frame_too_large_for_the_limit_is_sent_without_its_payload_and_flagged()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ch", "channel", SpaceId);

        var huge = JsonDocument.Parse("{\"content\":\"" + new string('x', 6000) + "\"}").RootElement.Clone();
        await harness.Source.Publish(ChannelScope, MessageFrame(ChannelScope, 5, huge));
        var evt = await harness.ReceiveAsync("event");

        evt.GetProperty("payloadOmitted").GetBoolean().Should().BeTrue();
        evt.TryGetProperty("payload", out _).Should().BeFalse("the client refetches the message over REST");
        evt.GetProperty("streamVersion").GetInt64().Should().Be(5, "the stream version still lets the client see the gap");
    }

    [Fact]
    public async Task A_removed_member_loses_the_channel_subscription_on_the_access_signal()
    {
        var authorizer = new FakeAuthorizer();
        var access = new ChannelAccessSource();
        var options = RealtimeGatewayHarness.FastOptions();
        options.ReauthorizationInterval = TimeSpan.FromHours(1);
        await using var harness = new RealtimeGatewayHarness(options, authorizer, accessSource: access);
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ch", "channel", SpaceId);

        // The membership ended: the authorization query now denies the channel to this person.
        authorizer.Set(ChannelScope, RealtimeAuthorizationResult.Denied);
        await access.SignalAsync(RealtimeGatewayHarness.WorkspaceId, "Channel", SpaceId, RealtimeGatewayHarness.UserId);

        var notice = await harness.ReceiveAsync("access-changed");
        notice.GetProperty("workspaceId").GetGuid().Should().Be(RealtimeGatewayHarness.WorkspaceId);
        var error = await harness.ReceiveAsync("subscription-error");
        error.GetProperty("code").GetString().Should().Be("subscription_revoked");
        error.GetProperty("subscriptionId").GetString().Should().Be("ch");
        harness.Connection.SubscriptionCount.Should().Be(0);
        harness.Source.OpenChannelCount.Should().Be(0);
    }

    [Fact]
    public async Task A_channel_signal_leaves_the_subscriptions_of_other_channels_alone()
    {
        var authorizer = new FakeAuthorizer();
        var access = new ChannelAccessSource();
        var options = RealtimeGatewayHarness.FastOptions();
        options.ReauthorizationInterval = TimeSpan.FromHours(1);
        await using var harness = new RealtimeGatewayHarness(options, authorizer, accessSource: access);
        await harness.ReceiveAsync("ready");
        var other = Guid.NewGuid();
        await harness.SubscribeAsync("ch", "channel", SpaceId);
        await harness.SubscribeAsync("other", "channel", other);

        authorizer.Set(ChannelScope, RealtimeAuthorizationResult.Denied);
        await access.SignalAsync(RealtimeGatewayHarness.WorkspaceId, "Channel", SpaceId, subject: null);

        var error = await harness.ReceiveAsync("subscription-error");
        error.GetProperty("subscriptionId").GetString().Should().Be("ch");
        harness.Connection.SubscriptionCount.Should().Be(1, "the other channel was not re-authorized away");
    }

    [Fact]
    public async Task The_per_user_stream_delivers_the_unread_frame_of_its_own_person_only()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        var me = RealtimeGatewayHarness.UserId;
        var userScope = new RealtimeScope(RealtimeScopeKind.User, me);

        var ack = await harness.SubscribeAsync("me", "user", me);
        ack.GetProperty("streamKey").GetString().Should().Be($"user:notifications:{me:N}");

        var unread = JsonDocument.Parse($"{{\"spaceId\":\"{SpaceId}\",\"unread\":3,\"mentions\":0}}").RootElement.Clone();
        // A frame that names another person must never reach this stream, even if it were published here.
        await harness.Source.Publish(userScope, MessageFrame(userScope, 1, unread, "chat.unread.changed") with { ResourceId = Guid.NewGuid() });
        await harness.Source.Publish(userScope, MessageFrame(userScope, 2, unread, "chat.unread.changed") with { ResourceId = me });

        var evt = await harness.ReceiveAsync("event");
        evt.GetProperty("eventType").GetString().Should().Be("chat.unread.changed");
        evt.GetProperty("streamVersion").GetInt64().Should().Be(2);
        evt.GetProperty("payload").GetProperty("unread").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Event_frames_stay_within_the_frame_limit()
    {
        await using var harness = new RealtimeGatewayHarness();
        await harness.ReceiveAsync("ready");
        await harness.SubscribeAsync("ch", "channel", SpaceId);

        var content = new string('a', 2800);
        await harness.Source.Publish(ChannelScope, MessageFrame(ChannelScope, 1, MessagePayload(1, content)));
        var evt = await harness.ReceiveAsync("event");

        Encoding.UTF8.GetByteCount(evt.GetRawText()).Should().BeLessThanOrEqualTo(RealtimeProtocol.MaxFrameBytes);
    }

    private sealed class ChannelAccessSource : IRealtimeAccessSignalSource
    {
        private Func<RealtimeNotification, ValueTask>? _on;

        public Task<IAsyncDisposable> SubscribeAccessAsync(
            Guid workspaceId, Func<RealtimeNotification, ValueTask> onSignal, CancellationToken cancellationToken)
        {
            _on = onSignal;
            return Task.FromResult<IAsyncDisposable>(new Handle());
        }

        public ValueTask SignalAsync(Guid workspaceId, string resourceKind, Guid resourceId, Guid? subject) => _on!(new RealtimeNotification(
            Guid.NewGuid().ToString("N"), "access.changed", "AccessChanged", $"access:{resourceKind}:{resourceId:N}", 1,
            resourceKind, resourceId, workspaceId, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, 0, 1, "access.changed.v1",
            subject));

        private sealed class Handle : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
