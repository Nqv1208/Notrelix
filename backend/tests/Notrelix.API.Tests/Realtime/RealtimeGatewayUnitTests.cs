using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.API.Tests.Realtime;

public sealed class RealtimeProtocolTests
{
    private static readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static RealtimeProtocol.ClientMessage Parse(string json) =>
        RealtimeProtocol.ParseClientMessage(Encoding.UTF8.GetBytes(json));

    [Theory]
    [InlineData("workspace", RealtimeScopeKind.Workspace)]
    [InlineData("board", RealtimeScopeKind.Board)]
    public void Subscribe_parses_supported_scopes(string kind, RealtimeScopeKind expected)
    {
        var message = Parse(
            "{\"type\":\"subscribe\",\"subscriptionId\":\"s-1\",\"scope\":{\"kind\":\"" + kind + "\",\"id\":\"" + Id + "\"}}");

        message.Should().Be(new RealtimeProtocol.SubscribeMessage("s-1", new RealtimeScope(expected, Id)));
    }

    [Theory]
    [InlineData("""{"type":"subscribe","subscriptionId":"s","scope":{"kind":"account","id":"22222222-2222-2222-2222-222222222222"}}""")]
    [InlineData("""{"type":"subscribe","subscriptionId":"s","scope":{"kind":"board","id":"not-a-guid"}}""")]
    [InlineData("""{"type":"subscribe","subscriptionId":"s","scope":{"kind":"board","id":"00000000-0000-0000-0000-000000000000"}}""")]
    public void Unsupported_or_malformed_scope_is_rejected(string json)
    {
        Parse(json).Should().BeOfType<RealtimeProtocol.InvalidMessage>()
            .Which.Code.Should().Be("unsupported_scope");
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"type":1}""")]
    [InlineData("""{"type":"explode"}""")]
    [InlineData("""{"type":"subscribe","scope":{"kind":"board","id":"22222222-2222-2222-2222-222222222222"}}""")]
    [InlineData("""{"type":"subscribe","subscriptionId":"bad id!","scope":{}}""")]
    [InlineData("""{"type":"unsubscribe"}""")]
    public void Malformed_messages_never_throw_and_are_invalid(string json)
    {
        Parse(json).Should().BeOfType<RealtimeProtocol.InvalidMessage>();
    }

    [Fact]
    public void Subscription_ids_are_bounded()
    {
        RealtimeProtocol.IsValidSubscriptionId(new string('a', 64)).Should().BeTrue();
        RealtimeProtocol.IsValidSubscriptionId(new string('a', 65)).Should().BeFalse();
        RealtimeProtocol.IsValidSubscriptionId(null).Should().BeFalse();
    }

    [Fact]
    public void Event_frame_has_the_documented_notify_only_shape()
    {
        var scope = new RealtimeScope(RealtimeScopeKind.Board, Id);
        var notification = RealtimeGatewayHarness.Notification(scope, 9);

        using var document = JsonDocument.Parse(RealtimeProtocol.Event("sub-1", notification));
        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

        names.Should().BeEquivalentTo(new[]
        {
            "type", "subscriptionId", "schemaVersion", "eventId", "eventType", "changeKind", "streamKey",
            "streamVersion", "resourceKind", "resourceId", "resourceVersion", "workspaceId", "correlationId",
            "occurredAt", "payloadContract",
        });
        document.RootElement.GetProperty("streamKey").GetString().Should().Be($"board:Board:{Id:N}");
        document.RootElement.GetProperty("streamVersion").GetInt64().Should().Be(9);
    }

    [Fact]
    public void Scope_stream_key_matches_the_producer_stream_key_format()
    {
        // RealtimeChangeMapper stamps $"{namespace}:{kind}:{resourceId:N}".
        new RealtimeScope(RealtimeScopeKind.Workspace, Id).StreamKey.Should().Be($"workspace:Workspace:{Id:N}");
        new RealtimeScope(RealtimeScopeKind.Board, Id).StreamKey.Should().Be($"board:Board:{Id:N}");
    }
}

public sealed class RealtimeGatewayOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(Action<RealtimeGatewayOptions>? configure = null)
    {
        var options = new RealtimeGatewayOptions();
        configure?.Invoke(options);
        return new RealtimeGatewayOptionsValidator().Validate(null, options);
    }

    [Fact]
    public void Defaults_are_valid() => Validate().Succeeded.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Subscription_limit_must_be_bounded(int value)
        => Validate(o => o.MaxSubscriptionsPerConnection = value).Failed.Should().BeTrue();

    [Fact]
    public void Idle_timeout_must_exceed_the_ping_interval()
        => Validate(o => { o.PingInterval = TimeSpan.FromSeconds(30); o.IdleTimeout = TimeSpan.FromSeconds(30); })
            .Failed.Should().BeTrue();

    [Fact]
    public void Path_must_be_rooted()
        => Validate(o => o.Path = "realtime").Failed.Should().BeTrue();

    [Fact]
    public void Non_positive_durations_are_rejected()
        => Validate(o => o.ReauthorizationInterval = TimeSpan.Zero).Failed.Should().BeTrue();
}

public sealed class RealtimeOriginPolicyTests
{
    [Fact]
    public void Only_listed_origins_are_allowed()
    {
        var policy = new RealtimeOriginPolicy(["https://app.notrelix.com/"], allowWildcard: false);

        policy.IsAllowed("https://app.notrelix.com").Should().BeTrue();
        policy.IsAllowed("https://APP.notrelix.com/").Should().BeTrue();
        policy.IsAllowed("https://evil.example").Should().BeFalse();
        policy.IsAllowed("https://app.notrelix.com.evil.example").Should().BeFalse();
        policy.IsAllowed("null").Should().BeFalse();
    }

    [Fact]
    public void Wildcard_is_honored_only_where_allowed()
    {
        new RealtimeOriginPolicy(["*"], allowWildcard: true).IsAllowed("https://anything.example").Should().BeTrue();
        new RealtimeOriginPolicy(["*"], allowWildcard: false).IsAllowed("https://anything.example").Should().BeFalse();
    }

    [Fact]
    public void Missing_origin_is_a_non_browser_client_and_still_must_authenticate()
    {
        new RealtimeOriginPolicy([], allowWildcard: false).IsAllowed(null).Should().BeTrue();
    }
}

public sealed class RealtimeConnectionLimiterTests
{
    [Fact]
    public void Concurrent_connections_per_user_are_bounded_and_released()
    {
        var limiter = new RealtimeConnectionLimiter();
        var user = Guid.NewGuid();

        var first = limiter.TryAcquire(user, 2);
        var second = limiter.TryAcquire(user, 2);
        var third = limiter.TryAcquire(user, 2);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        third.Should().BeNull();
        limiter.Count(user).Should().Be(2, "a rejected attempt must not leak a slot");

        first!.Dispose();
        first.Dispose();
        limiter.Count(user).Should().Be(1, "double dispose releases once");
        limiter.TryAcquire(user, 2).Should().NotBeNull();

        limiter.TryAcquire(Guid.NewGuid(), 1).Should().NotBeNull("limits are per user");
    }
}
