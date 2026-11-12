using Microsoft.Extensions.Logging.Abstractions;

using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Realtime;

namespace Notrelix.API.Tests.Realtime;

public sealed class RealtimeTypingPresenceTests
{
    private static readonly Guid SpaceId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Alice = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Bob = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static RealtimeEphemeralBus NewBus()
        => new(new InProcessRealtimeEphemeralTransport(), NullLogger<RealtimeEphemeralBus>.Instance);

    private static RealtimeGatewayOptions Options(Action<RealtimeGatewayOptions>? tune = null)
    {
        var options = RealtimeGatewayHarness.FastOptions();
        tune?.Invoke(options);
        return options;
    }

    private static async Task<RealtimeGatewayHarness> JoinAsync(
        RealtimeEphemeralBus bus, Guid user, RealtimeGatewayOptions? options = null, FakeAuthorizer? authorizer = null)
    {
        var harness = new RealtimeGatewayHarness(options, authorizer, ephemeralBus: bus, userId: user);
        await harness.ReceiveAsync("ready");
        var ack = await harness.SubscribeAsync("sp", "channel", SpaceId);
        ack.GetProperty("type").GetString().Should().Be("subscribed");
        return harness;
    }

    /// <summary>
    /// True when no frame of the type arrives. A ping round trip is the barrier (a cancelled read would abort the
    /// socket): frames the server queued before the pong are read first.
    /// </summary>
    private static async Task<bool> NothingArrivesAsync(RealtimeGatewayHarness harness, string type, int waitMs = 250)
    {
        await Task.Delay(waitMs);
        await harness.SendAsync(new { type = "ping", sentAt = "barrier" });
        while (true)
        {
            var frame = await harness.ReceiveAsync();
            var kind = frame.GetProperty("type").GetString();
            if (kind == type)
            {
                return false;
            }

            if (kind == "pong")
            {
                return true;
            }
        }
    }

    [Fact]
    public async Task Typing_reaches_the_other_subscribers_and_never_the_sender()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob);

        await alice.SendAsync(new { type = "typing", subscriptionId = "sp" });

        var frame = await bob.ReceiveAsync("typing");
        frame.GetProperty("subscriptionId").GetString().Should().Be("sp");
        frame.GetProperty("userId").GetGuid().Should().Be(Alice);
        frame.GetProperty("expiresInMs").GetInt64().Should().Be(5000);
        (await NothingArrivesAsync(alice, "typing")).Should().BeTrue("the sender never hears its own typing");
    }

    [Fact]
    public async Task Typing_above_the_rate_limit_is_dropped()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob, Options(o => o.TypingMinInterval = TimeSpan.FromSeconds(30)));

        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });
        await alice.ReceiveAsync("typing");
        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });
        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });

        (await NothingArrivesAsync(alice, "typing")).Should().BeTrue();
    }

    [Fact]
    public async Task Typing_is_allowed_again_after_the_minimum_interval()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob, Options(o => o.TypingMinInterval = TimeSpan.FromMilliseconds(100)));

        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });
        await alice.ReceiveAsync("typing");
        await Task.Delay(250);
        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });

        (await alice.ReceiveAsync("typing")).GetProperty("userId").GetGuid().Should().Be(Bob);
    }

    [Fact]
    public async Task Typing_on_an_unknown_subscription_or_a_non_channel_scope_is_ignored_without_a_reply()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob);
        await bob.SubscribeAsync("ws", "workspace", RealtimeGatewayHarness.WorkspaceId);

        await bob.SendAsync(new { type = "typing", subscriptionId = "nope" });
        await bob.SendAsync(new { type = "typing", subscriptionId = "ws" });

        (await NothingArrivesAsync(alice, "typing")).Should().BeTrue();
        (await NothingArrivesAsync(bob, "subscription-error")).Should().BeTrue("an unauthorized typing frame is ignored, not answered");
    }

    [Fact]
    public async Task Typing_is_ignored_once_the_scope_authorizer_no_longer_grants_the_space()
    {
        var bus = NewBus();
        var authorizer = new FakeAuthorizer();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob, Options(o => o.ReauthorizationInterval = TimeSpan.FromMinutes(10)), authorizer);

        authorizer.Set(new RealtimeScope(RealtimeScopeKind.Channel, SpaceId), RealtimeAuthorizationResult.Denied);
        await bob.SendAsync(new { type = "typing", subscriptionId = "sp" });

        (await NothingArrivesAsync(alice, "typing")).Should().BeTrue("an archived or left space ignores typing");
    }

    [Fact]
    public async Task Malformed_typing_and_presence_frames_are_rejected_as_invalid()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);

        await alice.SendAsync(new { type = "presence", state = "dancing" });
        (await alice.ReceiveAsync("subscription-error")).GetProperty("code").GetString().Should().Be("invalid_message");
        await alice.SendAsync(new { type = "typing" });
        (await alice.ReceiveAsync("subscription-error")).GetProperty("code").GetString().Should().Be("invalid_message");
    }

    [Fact]
    public async Task Presence_heartbeats_reach_the_other_subscribers_with_the_state_and_the_time_to_live()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob);

        await bob.SendAsync(new { type = "presence", state = "idle" });

        var frame = await alice.ReceiveAsync("presence");
        frame.GetProperty("userId").GetGuid().Should().Be(Bob);
        frame.GetProperty("state").GetString().Should().Be("idle");
        frame.GetProperty("ttlMs").GetInt64().Should().Be(75000);
        (await NothingArrivesAsync(bob, "presence")).Should().BeTrue();
    }

    [Fact]
    public async Task A_late_subscriber_learns_who_is_here_without_any_stored_state()
    {
        var bus = NewBus();
        await using var bob = await JoinAsync(bus, Bob);
        await bob.SendAsync(new { type = "presence", state = "online" });

        // A gateway "restart" for Alice: a new connection with no memory of earlier heartbeats.
        await using var alice = await JoinAsync(bus, Alice);

        var frame = await alice.ReceiveAsync("presence");
        frame.GetProperty("userId").GetGuid().Should().Be(Bob);
        frame.GetProperty("state").GetString().Should().Be("online");
    }

    [Fact]
    public async Task Presence_without_a_heartbeat_expires_to_offline_and_closing_announces_offline()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob, Options(o => o.PresenceTtl = TimeSpan.FromMilliseconds(300)));

        await bob.SendAsync(new { type = "presence", state = "online" });
        (await alice.ReceiveAsync("presence")).GetProperty("state").GetString().Should().Be("online");

        var expired = await alice.ReceiveAsync("presence");
        expired.GetProperty("userId").GetGuid().Should().Be(Bob);
        expired.GetProperty("state").GetString().Should().Be("offline");

        await bob.SendAsync(new { type = "presence", state = "online" });
        await alice.ReceiveAsync("presence");
        bob.Connection.RequestClose(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "tab_closed");
        (await alice.ReceiveAsync("presence")).GetProperty("state").GetString().Should().Be("offline");
    }

    [Fact]
    public async Task Presence_frames_above_the_rate_limit_are_dropped_unless_the_state_changes()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice);
        await using var bob = await JoinAsync(bus, Bob, Options(o => o.PresenceMinInterval = TimeSpan.FromSeconds(30)));

        await bob.SendAsync(new { type = "presence", state = "online" });
        await alice.ReceiveAsync("presence");
        await bob.SendAsync(new { type = "presence", state = "online" });
        (await NothingArrivesAsync(alice, "presence")).Should().BeTrue();

        await bob.SendAsync(new { type = "presence", state = "idle" });
        (await alice.ReceiveAsync("presence")).GetProperty("state").GetString().Should().Be("idle");
    }

    [Fact]
    public async Task Per_connection_state_stays_bounded_by_the_subscription_cap()
    {
        var bus = NewBus();
        await using var alice = await JoinAsync(bus, Alice, Options(o => o.MaxSubscriptionsPerConnection = 2));

        await alice.SubscribeAsync("a", "channel", Guid.NewGuid());
        var denied = await alice.SubscribeAsync("b", "channel", Guid.NewGuid());

        denied.GetProperty("code").GetString().Should().Be("subscription_limit");
    }

    [Fact]
    public async Task Without_an_ephemeral_bus_the_frames_are_accepted_and_do_nothing()
    {
        await using var alice = new RealtimeGatewayHarness(userId: Alice);
        await alice.ReceiveAsync("ready");
        await alice.SubscribeAsync("sp", "channel", SpaceId);

        await alice.SendAsync(new { type = "typing", subscriptionId = "sp" });
        await alice.SendAsync(new { type = "presence", state = "online" });
        await alice.SendAsync(new { type = "ping" });

        (await alice.ReceiveAsync("pong")).GetProperty("type").GetString().Should().Be("pong");
    }
}
