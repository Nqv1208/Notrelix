using System.Text;
using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Application.Tests.Features.Chat.Realtime;

/// <summary>UC-CHT-C02, C03, C04 and C09 at the level of the realtime facts: the frame identity, the compact payload and its size budget.</summary>
public class ChatRealtimeFactsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid SpaceId = Guid.NewGuid();
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();

    private static ChatMessageCreatedIntegrationEvent Created(string content, long sequence = 7, Guid? replyTo = null) => new(
        EventId: Guid.NewGuid(),
        AccountId: AccountId,
        WorkspaceId: WorkspaceId,
        MessageId: MessageId,
        SpaceId: SpaceId,
        Sequence: sequence,
        AuthorId: AuthorId,
        Kind: "user",
        Content: content,
        ClientMessageId: "c-1",
        Version: 1,
        CorrelationId: Guid.NewGuid(),
        ActorUserId: AuthorId,
        OccurredAt: Now,
        ReplyToId: replyTo);

    private static JsonElement Payload(RealtimeResourceChangedV1 change) => change.Payload;

    [Fact]
    public void A_created_message_is_a_channel_frame_with_the_compact_payload_and_the_sequence()
    {
        var change = ChatRealtime.ForCreated(Created("hello", sequence: 7, replyTo: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), Now);

        change.TopicNamespace.Should().Be("chat");
        change.ResourceKind.Should().Be("Channel");
        change.ResourceId.Should().Be(SpaceId);
        change.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");
        change.ChangeKind.Should().Be("chat.message.created");
        change.PayloadContract.Should().Be("chat.message.v1");

        var payload = Payload(change);
        payload.GetProperty("id").GetGuid().Should().Be(MessageId);
        payload.GetProperty("sequence").GetInt64().Should().Be(7);
        payload.GetProperty("authorId").GetGuid().Should().Be(AuthorId);
        payload.GetProperty("kind").GetString().Should().Be("user");
        payload.GetProperty("content").GetString().Should().Be("hello");
        payload.GetProperty("contentOmitted").GetBoolean().Should().BeFalse();
        payload.GetProperty("deleted").GetBoolean().Should().BeFalse();
        payload.GetProperty("createdAt").GetDateTimeOffset().Should().Be(Now);
        payload.TryGetProperty("editedAt", out _).Should().BeFalse("a null field is left out of the frame");
        payload.GetProperty("replyToId").GetGuid().Should().Be(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        payload.GetProperty("version").GetInt64().Should().Be(1);
    }

    [Fact]
    public void A_thread_reply_is_a_channel_frame_that_names_its_root_and_its_thread_sequence()
    {
        // The reply goes to the channel stream like any message; the root and the thread sequence let a client route it
        // to the thread and count the replies it holds (UC-CHT-F01, C02).
        var root = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var reply = Created("in thread") with { ThreadRootId = root, ThreadSequence = 3 };

        var change = ChatRealtime.ForCreated(reply, Now);

        change.ChangeKind.Should().Be("chat.message.created");
        change.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");
        var payload = Payload(change);
        payload.GetProperty("threadRootId").GetGuid().Should().Be(root);
        payload.GetProperty("threadSequence").GetInt64().Should().Be(3);
    }

    [Fact]
    public void A_created_frame_carries_the_senders_client_message_id_and_an_edit_or_delete_does_not()
    {
        Payload(ChatRealtime.ForCreated(Created("hello"), Now)).GetProperty("clientMessageId").GetString().Should().Be("c-1");

        var edited = new ChatMessageUpdatedIntegrationEvent(
            Guid.NewGuid(), AccountId, WorkspaceId, MessageId, SpaceId, 7, AuthorId, "fixed", 2, Guid.NewGuid(),
            ActorUserId: AuthorId, OccurredAt: Now);
        Payload(ChatRealtime.ForUpdated(edited, Now)).TryGetProperty("clientMessageId", out _).Should().BeFalse();
    }

    [Fact]
    public void A_channel_message_has_no_thread_fields_in_its_payload()
    {
        var payload = Payload(ChatRealtime.ForCreated(Created("plain"), Now));

        payload.TryGetProperty("threadRootId", out _).Should().BeFalse("a null field is left out of the frame");
        payload.TryGetProperty("threadSequence", out _).Should().BeFalse("a null field is left out of the frame");
    }

    [Fact]
    public void An_edit_carries_the_new_content_and_the_edit_time_and_the_version()
    {
        var edited = new ChatMessageUpdatedIntegrationEvent(
            Guid.NewGuid(), AccountId, WorkspaceId, MessageId, SpaceId, 7, AuthorId, "fixed", 2, Guid.NewGuid(),
            ActorUserId: AuthorId, OccurredAt: Now.AddMinutes(1));

        var change = ChatRealtime.ForUpdated(edited, Now);

        change.ChangeKind.Should().Be("chat.message.updated");
        var payload = Payload(change);
        payload.GetProperty("content").GetString().Should().Be("fixed");
        payload.GetProperty("editedAt").GetDateTimeOffset().Should().Be(Now.AddMinutes(1));
        payload.GetProperty("version").GetInt64().Should().Be(2);
        payload.GetProperty("sequence").GetInt64().Should().Be(7);
        payload.GetProperty("deleted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void A_delete_is_a_tombstone_with_null_content_that_keeps_its_identity_and_sequence()
    {
        var deleted = new ChatMessageDeletedIntegrationEvent(
            Guid.NewGuid(), AccountId, WorkspaceId, MessageId, SpaceId, 7, AuthorId, AuthorId, 3, Guid.NewGuid(),
            ActorUserId: AuthorId, OccurredAt: Now.AddMinutes(2));

        var change = ChatRealtime.ForDeleted(deleted, Now);

        change.ChangeKind.Should().Be("chat.message.deleted");
        var payload = Payload(change);
        payload.TryGetProperty("content", out _).Should().BeFalse("a null field is left out of the frame");
        payload.GetProperty("deleted").GetBoolean().Should().BeTrue();
        payload.GetProperty("id").GetGuid().Should().Be(MessageId);
        payload.GetProperty("sequence").GetInt64().Should().Be(7);
        payload.GetProperty("version").GetInt64().Should().Be(3);
    }

    [Fact]
    public void Content_that_would_break_the_frame_budget_is_left_out_and_flagged()
    {
        // 4,000 characters of multi-byte text: far above the 3,000-byte payload budget once escaped to UTF-8.
        var content = new string('é', 3900);

        var change = ChatRealtime.ForCreated(Created(content), Now);

        var payload = Payload(change);
        payload.TryGetProperty("content", out _).Should().BeFalse("a null field is left out of the frame");
        payload.GetProperty("contentOmitted").GetBoolean().Should().BeTrue();
        payload.GetProperty("sequence").GetInt64().Should().Be(7, "the client refetches by sequence");
        Encoding.UTF8.GetByteCount(payload.GetRawText()).Should().BeLessThanOrEqualTo(ChatRealtime.MaxPayloadBytes);
    }

    [Fact]
    public void Content_that_fits_is_kept_whole()
    {
        var content = new string('a', ChatRealtime.MaxPayloadBytes / 2);

        var payload = Payload(ChatRealtime.ForCreated(Created(content), Now));

        payload.GetProperty("content").GetString().Should().Be(content);
        payload.GetProperty("contentOmitted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void The_preview_text_collapses_whitespace_and_stops_at_140_characters_without_splitting_a_pair()
    {
        ChatRealtime.PreviewText("  hello \n  world  ").Should().Be("hello world");
        ChatRealtime.PreviewText("   ").Should().BeNull();
        ChatRealtime.PreviewText(null).Should().BeNull();
        ChatRealtime.PreviewText(new string('a', 500)).Should().HaveLength(ChatRealtime.PreviewMaxLength);
        // 139 letters then an emoji (a surrogate pair): the cut at 140 would split it, so it is dropped whole.
        ChatRealtime.PreviewText(new string('a', 139) + "\U0001F600 tail").Should().Be(new string('a', 139));
    }

    [Fact]
    public void A_per_user_unread_fact_names_the_space_and_the_counts_on_the_person_stream()
    {
        var userId = Guid.NewGuid();

        var change = ChatRealtime.Unread(AccountId, WorkspaceId, userId, SpaceId, unread: 4, mentions: 1, null, null, Now);

        change.TopicNamespace.Should().Be("user");
        change.ResourceKind.Should().Be("notifications");
        change.ResourceId.Should().Be(userId);
        change.StreamKey.Should().Be($"user:notifications:{userId:N}");
        change.ChangeKind.Should().Be("chat.unread.changed");
        change.PayloadContract.Should().Be("chat.unread.v1");
        change.WorkspaceId.Should().Be(WorkspaceId);
        var payload = Payload(change);
        payload.GetProperty("spaceId").GetGuid().Should().Be(SpaceId);
        payload.GetProperty("unread").GetInt32().Should().Be(4);
        payload.GetProperty("mentions").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Channel_and_user_scopes_map_to_their_own_streams_and_wire_kinds()
    {
        var channel = new RealtimeScope(RealtimeScopeKind.Channel, SpaceId);
        channel.WireKind.Should().Be("channel");
        channel.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");

        var user = new RealtimeScope(RealtimeScopeKind.User, AuthorId);
        user.WireKind.Should().Be("user");
        user.StreamKey.Should().Be($"user:notifications:{AuthorId:N}");

        RealtimeScope.TryParse("channel", SpaceId.ToString(), out var parsed).Should().BeTrue();
        parsed.Should().Be(channel);
    }
}
