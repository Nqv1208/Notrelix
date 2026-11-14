using Microsoft.Extensions.Logging.Abstractions;

using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Realtime;

namespace Notrelix.Infrastructure.Tests.Realtime;

public sealed class RealtimeEphemeralBusTests
{
    private static readonly RealtimeScope Space = new(RealtimeScopeKind.Channel, Guid.NewGuid());
    private static readonly Guid Workspace = Guid.NewGuid();

    private static RealtimeEphemeralBus NewBus(IRealtimeEphemeralTransport? transport = null)
        => new(transport ?? new InProcessRealtimeEphemeralTransport(), NullLogger<RealtimeEphemeralBus>.Instance);

    [Fact]
    public async Task A_published_signal_reaches_every_subscriber_of_the_scope_only()
    {
        var bus = NewBus();
        var first = new List<RealtimeEphemeralSignal>();
        var second = new List<RealtimeEphemeralSignal>();
        var other = new List<RealtimeEphemeralSignal>();
        await using var a = await bus.SubscribeAsync(Space, s => { first.Add(s); return ValueTask.CompletedTask; }, default);
        await using var b = await bus.SubscribeAsync(Space, s => { second.Add(s); return ValueTask.CompletedTask; }, default);
        await using var c = await bus.SubscribeAsync(
            new RealtimeScope(RealtimeScopeKind.Channel, Guid.NewGuid()), s => { other.Add(s); return ValueTask.CompletedTask; }, default);

        var user = Guid.NewGuid();
        await bus.PublishAsync(Space, new RealtimeEphemeralSignal("typing", Workspace, user), default);

        first.Should().ContainSingle().Which.UserId.Should().Be(user);
        second.Should().ContainSingle();
        other.Should().BeEmpty();
    }

    [Fact]
    public async Task One_transport_subscription_is_shared_and_closed_with_the_last_handler()
    {
        var bus = NewBus();
        var a = await bus.SubscribeAsync(Space, _ => ValueTask.CompletedTask, default);
        var b = await bus.SubscribeAsync(Space, _ => ValueTask.CompletedTask, default);
        bus.OpenChannelCount.Should().Be(1);

        await a.DisposeAsync();
        bus.OpenChannelCount.Should().Be(1);
        await b.DisposeAsync();
        bus.OpenChannelCount.Should().Be(0);
    }

    [Fact]
    public async Task Nothing_is_retained_a_signal_published_without_listeners_is_gone()
    {
        var bus = NewBus();
        await bus.PublishAsync(Space, new RealtimeEphemeralSignal("presence", Workspace, Guid.NewGuid(), "online"), default);

        var seen = new List<RealtimeEphemeralSignal>();
        await using var late = await bus.SubscribeAsync(Space, s => { seen.Add(s); return ValueTask.CompletedTask; }, default);

        seen.Should().BeEmpty();
    }

    [Fact]
    public async Task A_malformed_message_is_dropped()
    {
        var transport = new InProcessRealtimeEphemeralTransport();
        var bus = NewBus(transport);
        var seen = new List<RealtimeEphemeralSignal>();
        await using var sub = await bus.SubscribeAsync(Space, s => { seen.Add(s); return ValueTask.CompletedTask; }, default);

        await transport.PublishAsync(
            RealtimeChannelResolver.RedisChannel(RealtimeEphemeralBus.ChannelOf(Space)), "{not json");
        await transport.PublishAsync(
            RealtimeChannelResolver.RedisChannel(RealtimeEphemeralBus.ChannelOf(Space)),
            "{\"kind\":\"typing\",\"workspaceId\":\"00000000-0000-0000-0000-000000000000\",\"userId\":\"00000000-0000-0000-0000-000000000000\"}");

        seen.Should().BeEmpty();
    }
}
