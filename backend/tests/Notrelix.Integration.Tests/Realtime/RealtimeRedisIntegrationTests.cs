using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Infrastructure.Realtime;
using Notrelix.Integration.Tests.Containers;

using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Realtime;

/// <summary>
/// WRITTEN, NOT EXECUTED in the authoring session (Docker was unavailable).
/// Proves against a real Redis that the publisher's atomic counter script gives every
/// stream consecutive, in-order versions under concurrent publication, that streams are
/// independent, and that the source delivers only the subscribed stream.
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class RealtimeRedisIntegrationTests
{
    private readonly CacheTestContainer _containers;

    public RealtimeRedisIntegrationTests(CacheTestContainer containers)
    {
        _containers = containers;
    }

    private static RealtimeResourceChangedV1 Change(Guid workspaceId, string ns, string kind, Guid resourceId) => new(
        Guid.NewGuid(), Guid.NewGuid(), workspaceId, Guid.NewGuid(), Guid.NewGuid(), null,
        DateTimeOffset.UtcNow, ns, kind, resourceId, $"{ns}:{kind}:{resourceId:N}", 1, "Change", "Dto.v1",
        JsonSerializer.SerializeToElement(new { }));

    [Fact]
    public async Task Concurrent_publishes_deliver_consecutive_in_order_versions_per_stream_and_isolate_streams()
    {
        await _containers.ResetAsync();
        await using var redis = await ConnectionMultiplexer.ConnectAsync(_containers.RedisConnectionString);
        var publisher = new RedisRealtimePublisher(redis, NullLogger<RedisRealtimePublisher>.Instance, new MetricsService());
        var source = new RedisRealtimeNotificationSource(redis, NullLogger<RedisRealtimeNotificationSource>.Instance);

        var workspaceId = Guid.NewGuid();
        var boardA = new RealtimeScope(RealtimeScopeKind.Board, Guid.NewGuid());
        var boardB = new RealtimeScope(RealtimeScopeKind.Board, Guid.NewGuid());
        var receivedA = new ConcurrentQueue<long>();
        var receivedB = new ConcurrentQueue<long>();
        await using var subscriptionA = await source.SubscribeAsync(boardA, n => { receivedA.Enqueue(n.StreamVersion); return ValueTask.CompletedTask; }, default);
        await using var subscriptionB = await source.SubscribeAsync(boardB, n => { receivedB.Enqueue(n.StreamVersion); return ValueTask.CompletedTask; }, default);

        const int perStream = 200;
        await Task.WhenAll(Enumerable.Range(0, perStream).SelectMany(_ => new[]
        {
            publisher.PublishAsync(Change(workspaceId, "board", "Board", boardA.Id), default),
            publisher.PublishAsync(Change(workspaceId, "board", "Board", boardB.Id), default),
        }));

        await WaitUntilAsync(() => receivedA.Count == perStream && receivedB.Count == perStream);

        receivedA.Should().Equal(Enumerable.Range(1, perStream).Select(i => (long)i));
        receivedB.Should().Equal(Enumerable.Range(1, perStream).Select(i => (long)i));
    }

    [Fact]
    public async Task Reset_counter_is_visible_to_the_client_as_a_non_consecutive_version()
    {
        await _containers.ResetAsync();
        await using var redis = await ConnectionMultiplexer.ConnectAsync(_containers.RedisConnectionString);
        var publisher = new RedisRealtimePublisher(redis, NullLogger<RedisRealtimePublisher>.Instance, new MetricsService());
        var source = new RedisRealtimeNotificationSource(redis, NullLogger<RedisRealtimeNotificationSource>.Instance);
        var scope = new RealtimeScope(RealtimeScopeKind.Board, Guid.NewGuid());
        var received = new ConcurrentQueue<long>();
        await using var subscription = await source.SubscribeAsync(scope, n => { received.Enqueue(n.StreamVersion); return ValueTask.CompletedTask; }, default);

        var change = Change(Guid.NewGuid(), "board", "Board", scope.Id);
        await publisher.PublishAsync(change, default);
        await publisher.PublishAsync(change, default);
        await redis.GetDatabase().KeyDeleteAsync(RealtimeChannelResolver.StreamCounterKey(scope.StreamKey));
        await publisher.PublishAsync(change, default);

        await WaitUntilAsync(() => received.Count == 3);
        received.Should().Equal(1L, 2L, 1L); // client rule: anything but previous + 1 means refetch
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        condition().Should().BeTrue("the expected notifications must arrive before the deadline");
    }
}
