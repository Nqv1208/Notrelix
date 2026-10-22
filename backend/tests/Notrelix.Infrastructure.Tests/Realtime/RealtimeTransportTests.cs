using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Infrastructure.Realtime;

using StackExchange.Redis;

namespace Notrelix.Infrastructure.Tests.Realtime;

public sealed class RealtimeChannelResolverTests
{
    private static readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(RealtimeScopeKind.Workspace)]
    [InlineData(RealtimeScopeKind.Board)]
    public void Publisher_and_subscriber_resolve_the_same_channel_for_a_scope(RealtimeScopeKind kind)
    {
        var scope = new RealtimeScope(kind, Id);

        RealtimeChannelResolver.ForScope(scope).Should().Be(RealtimeChannelResolver.ForTopic(scope.Topic));
    }

    [Fact]
    public void Workspace_and_board_streams_never_share_a_channel_or_counter()
    {
        var workspace = new RealtimeScope(RealtimeScopeKind.Workspace, Id);
        var board = new RealtimeScope(RealtimeScopeKind.Board, Id);

        RealtimeChannelResolver.ForScope(workspace).Should().NotBe(RealtimeChannelResolver.ForScope(board));
        RealtimeChannelResolver.StreamCounterKey(workspace.StreamKey)
            .Should().NotBe(RealtimeChannelResolver.StreamCounterKey(board.StreamKey));
    }
}

public sealed class RealtimeNotificationJsonTests
{
    private static RealtimeNotification Draft() => new(
        "e1", "board.board", "CreateBoardItem", "board:Board:abc", 0, "Board", Guid.NewGuid(), Guid.NewGuid(),
        "c1", DateTimeOffset.Parse("2026-10-20T10:00:00Z"), 4, 1, "Contract.v1");

    [Fact]
    public void Spliced_version_produces_valid_json_that_round_trips()
    {
        var (prefix, suffix) = RealtimeNotificationJson.SerializeForVersionSplice(Draft());

        foreach (var version in new long[] { 1, 2, 9, 10, 1_000_000_000_000 })
        {
            RealtimeNotificationJson.TryDeserialize(prefix + version + suffix, out var parsed).Should().BeTrue();
            parsed.StreamVersion.Should().Be(version);
            parsed.StreamKey.Should().Be("board:Board:abc");
            parsed.ChangeKind.Should().Be("CreateBoardItem");
            parsed.ResourceVersion.Should().Be(4);
        }
    }

    [Fact]
    public void Wire_message_never_carries_a_resource_payload()
    {
        var json = RealtimeNotificationJson.Serialize(Draft() with { StreamVersion = 3 });

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("payload", out _).Should().BeFalse();
        document.RootElement.EnumerateObject().Last().Name.Should().Be("streamVersion");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"eventId":"e","streamKey":"k","streamVersion":0,"workspaceId":"22222222-2222-2222-2222-222222222222","schemaVersion":1}""")]
    [InlineData("""{"eventId":"e","streamKey":"k","streamVersion":1,"workspaceId":"00000000-0000-0000-0000-000000000000","schemaVersion":1}""")]
    [InlineData("""{"eventId":"e","streamKey":"k","streamVersion":1,"workspaceId":"22222222-2222-2222-2222-222222222222","schemaVersion":2}""")]
    public void Malformed_foreign_or_tenantless_messages_are_rejected(string json)
    {
        RealtimeNotificationJson.TryDeserialize(json, out _).Should().BeFalse();
    }
}

public sealed class RedisRealtimePublisherTests
{
    private static RealtimeResourceChangedV1 Change(Guid? workspaceId, string ns = "board", string kind = "Board", Guid? resourceId = null)
    {
        var id = resourceId ?? Guid.NewGuid();
        return new RealtimeResourceChangedV1(
            Guid.NewGuid(), Guid.NewGuid(), workspaceId, Guid.NewGuid(), Guid.NewGuid(), null,
            DateTimeOffset.UtcNow, ns, kind, id, $"{ns}:{kind}:{id:N}", 12, "CreateBoardItem", "Dto.v1",
            JsonSerializer.SerializeToElement(new { secret = "must-not-be-published" }));
    }

    private static (RedisRealtimePublisher Publisher, Mock<IDatabase> Database, List<(RedisKey[] Keys, RedisValue[] Values)> Calls) Create()
    {
        var calls = new List<(RedisKey[], RedisValue[])>();
        var database = new Mock<IDatabase>();
        database.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, keys, values, _) => calls.Add((keys, values)))
            .ReturnsAsync(RedisResult.Create((RedisValue)1));
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var publisher = new RedisRealtimePublisher(
            redis.Object, NullLogger<RedisRealtimePublisher>.Instance, new MetricsService());
        return (publisher, database, calls);
    }

    [Fact]
    public async Task Publishes_one_atomic_script_with_counter_and_channel_keys_and_no_payload()
    {
        var (publisher, _, calls) = Create();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        await publisher.PublishAsync(Change(workspaceId, resourceId: boardId), CancellationToken.None);

        var (keys, values) = calls.Should().ContainSingle().Subject;
        var scope = new RealtimeScope(RealtimeScopeKind.Board, boardId);
        keys[0].ToString().Should().Be(RealtimeChannelResolver.StreamCounterKey(scope.StreamKey));
        keys[1].ToString().Should().Be(RealtimeChannelResolver.RedisChannel(RealtimeChannelResolver.ForScope(scope)));

        var message = values[0].ToString() + 5 + values[1];
        message.Should().NotContain("must-not-be-published");
        RealtimeNotificationJson.TryDeserialize(message, out var parsed).Should().BeTrue();
        parsed.WorkspaceId.Should().Be(workspaceId);
        parsed.StreamKey.Should().Be(scope.StreamKey);
        parsed.StreamVersion.Should().Be(5, "the stream version is the atomically allocated counter");
        parsed.ResourceVersion.Should().Be(12, "the producer's resource version stays informational");
        parsed.ChangeKind.Should().Be("CreateBoardItem");
    }

    [Fact]
    public async Task Workspace_namespace_events_publish_on_the_workspace_channel()
    {
        var (publisher, _, calls) = Create();
        var workspaceId = Guid.NewGuid();

        await publisher.PublishAsync(Change(workspaceId, "workspace", "Workspace", workspaceId), CancellationToken.None);

        calls[0].Keys[1].ToString().Should().Be(
            RealtimeChannelResolver.RedisChannel(RealtimeChannelResolver.Workspace(workspaceId)));
    }

    [Fact]
    public async Task Tenantless_events_are_never_published()
    {
        var (publisher, _, calls) = Create();

        await publisher.PublishAsync(Change(null), CancellationToken.None);
        await publisher.PublishAsync(Change(Guid.Empty), CancellationToken.None);

        calls.Should().BeEmpty();
    }
}

public sealed class RedisRealtimeNotificationSourceTests
{
    private static readonly RealtimeScope Scope = new(RealtimeScopeKind.Board, Guid.Parse("44444444-4444-4444-4444-444444444444"));

    private static (RedisRealtimeNotificationSource Source, Mock<ISubscriber> Subscriber, Func<string, Task> Deliver) Create()
    {
        Action<RedisChannel, RedisValue>? handler = null;
        var subscriber = new Mock<ISubscriber>();
        subscriber.Setup(s => s.SubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((_, h, _) => handler = h)
            .Returns(Task.CompletedTask);
        subscriber.Setup(s => s.UnsubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>?>(), It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(subscriber.Object);
        var source = new RedisRealtimeNotificationSource(redis.Object, NullLogger<RedisRealtimeNotificationSource>.Instance);
        return (source, subscriber, json =>
        {
            handler!(default, json);
            return Task.CompletedTask;
        });
    }

    private static string Message(long version) => RealtimeNotificationJson.Serialize(new RealtimeNotification(
        "e" + version, "board.board", "CreateBoardItem", Scope.StreamKey, version, "Board", Scope.Id,
        Guid.NewGuid(), "c", DateTimeOffset.UtcNow, 1, 1, "Dto.v1"));

    [Fact]
    public async Task Handlers_share_one_redis_subscription_and_it_closes_with_the_last_handler()
    {
        var (source, subscriber, deliver) = Create();
        var first = new List<long>();
        var second = new List<long>();

        var a = await source.SubscribeAsync(Scope, n => { first.Add(n.StreamVersion); return ValueTask.CompletedTask; }, CancellationToken.None);
        var b = await source.SubscribeAsync(Scope, n => { second.Add(n.StreamVersion); return ValueTask.CompletedTask; }, CancellationToken.None);

        subscriber.Verify(s => s.SubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()), Times.Once);

        await deliver(Message(1));
        await deliver(Message(2));
        first.Should().Equal(1, 2);
        second.Should().Equal(1, 2);

        await a.DisposeAsync();
        subscriber.Verify(s => s.UnsubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>?>(), It.IsAny<CommandFlags>()), Times.Never);
        await deliver(Message(3));
        first.Should().Equal(1, 2);
        second.Should().Equal(1, 2, 3);

        await b.DisposeAsync();
        subscriber.Verify(s => s.UnsubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>?>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task Malformed_messages_are_dropped_and_a_failing_handler_does_not_stop_others()
    {
        var (source, _, deliver) = Create();
        var received = new List<long>();
        await source.SubscribeAsync(Scope, _ => throw new InvalidOperationException("boom"), CancellationToken.None);
        await source.SubscribeAsync(Scope, n => { received.Add(n.StreamVersion); return ValueTask.CompletedTask; }, CancellationToken.None);

        await deliver("garbage");
        await deliver(Message(7));
        await Task.Delay(50);

        received.Should().Equal(7);
    }
}
