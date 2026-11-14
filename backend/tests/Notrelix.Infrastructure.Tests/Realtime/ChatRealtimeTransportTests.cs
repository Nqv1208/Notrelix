using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Features.Chat.Realtime;
using Notrelix.Infrastructure.Realtime;

namespace Notrelix.Infrastructure.Tests.Realtime;

/// <summary>The chat frames over the transports: the payload survives the Redis wire format, and only chat and user topics carry one.</summary>
public class ChatRealtimeTransportTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid SpaceId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003");

    private static RealtimeResourceChangedV1 MessageChange(string content) => new(
        Guid.NewGuid(), Guid.NewGuid(), WorkspaceId, UserId, Guid.NewGuid(), null, Now,
        ChatRealtime.ChannelTopicNamespace, ChatRealtime.ChannelResourceKind, SpaceId,
        $"chat:Channel:{SpaceId:N}", 1, ChatRealtime.MessageCreatedEvent, ChatRealtime.MessagePayloadContract,
        JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), sequence = 3, content, contentOmitted = false, deleted = false }));

    [Fact]
    public void A_chat_frame_keeps_its_payload_across_the_redis_wire_format()
    {
        var change = MessageChange("hello \"quoted\" ,\"streamVersion\": text");
        var draft = new RealtimeNotification(
            EventId: change.EventId.ToString("N"),
            EventType: RealtimeFrameMapping.EventTypeOf(change),
            ChangeKind: change.ChangeKind,
            StreamKey: change.StreamKey,
            StreamVersion: 0,
            ResourceKind: change.ResourceKind,
            ResourceId: change.ResourceId,
            WorkspaceId: WorkspaceId,
            CorrelationId: change.CorrelationId.ToString("N"),
            OccurredAt: Now,
            ResourceVersion: change.StreamVersion,
            SchemaVersion: 1,
            PayloadContract: change.PayloadContract,
            Payload: RealtimeFrameMapping.PayloadOf(change));

        var (prefix, suffix) = RealtimeNotificationJson.SerializeForVersionSplice(draft);
        var json = prefix + 9 + suffix;

        RealtimeNotificationJson.TryDeserialize(json, out var received).Should().BeTrue();
        received.StreamVersion.Should().Be(9, "the version is spliced in by the Redis script");
        received.EventType.Should().Be("chat.message.created");
        received.Payload.Should().NotBeNull();
        received.Payload!.Value.GetProperty("content").GetString().Should().Be("hello \"quoted\" ,\"streamVersion\": text",
            "the payload text is not confused with the version property");
    }

    [Fact]
    public void Chat_and_user_topics_carry_a_payload_and_board_frames_stay_notify_only()
    {
        var chat = MessageChange("hi");
        RealtimeFrameMapping.PayloadOf(chat).Should().NotBeNull();
        RealtimeFrameMapping.ScopeOf(chat).Should().Be(new RealtimeScope(RealtimeScopeKind.Channel, SpaceId));

        var unread = ChatRealtime.Unread(Guid.NewGuid(), WorkspaceId, UserId, SpaceId, 2, 0, null, null, Now);
        RealtimeFrameMapping.PayloadOf(unread).Should().NotBeNull();
        RealtimeFrameMapping.ScopeOf(unread).Should().Be(new RealtimeScope(RealtimeScopeKind.User, UserId));
        RealtimeFrameMapping.EventTypeOf(unread).Should().Be("chat.unread.changed");

        var board = new RealtimeResourceChangedV1(
            Guid.NewGuid(), null, WorkspaceId, UserId, Guid.NewGuid(), null, Now,
            "board", "Board", SpaceId, $"board:Board:{SpaceId:N}", 1, "CreateBoardItem", "board.changed.v1",
            JsonSerializer.SerializeToElement(new { }));
        RealtimeFrameMapping.PayloadOf(board).Should().BeNull("existing frames never carry resource data");
        RealtimeFrameMapping.EventTypeOf(board).Should().Be("board.board");
    }

    [Fact]
    public void Each_scope_resolves_to_its_own_channel_names()
    {
        RealtimeChannelResolver.ForScope(new RealtimeScope(RealtimeScopeKind.User, UserId))
            .Should().Be($"user:{UserId}:notifications");
        RealtimeChannelResolver.ForScope(new RealtimeScope(RealtimeScopeKind.Channel, SpaceId))
            .Should().Be($"resource:chat:Channel:{SpaceId}");
        RealtimeChannelResolver.ForScope(new RealtimeScope(RealtimeScopeKind.Board, SpaceId))
            .Should().Be($"resource:board:Board:{SpaceId}");
    }
}
