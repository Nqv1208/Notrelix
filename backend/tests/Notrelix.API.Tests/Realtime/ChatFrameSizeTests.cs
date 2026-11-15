using System.Text;
using System.Text.Json;
using Notrelix.API.Realtime;
using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;
using Xunit.Abstractions;

namespace Notrelix.API.Tests.Realtime;

/// <summary>
/// The bytes of every chat frame as the gateway puts them on the wire (event envelope plus payload). Each frame stays under
/// 2 KiB, however long the message text is: a text that does not fit is left out and the client fetches it by sequence.
/// </summary>
public sealed class ChatFrameSizeTests(ITestOutputHelper output)
{
    private const int Budget = 2048;
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 30, 12, 345, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly string SubscriptionId = "channel:" + Space;

    private static ChatMessageCreatedIntegrationEvent Created(string content, Guid? threadRoot = null) => new(
        EventId: Guid.NewGuid(), AccountId: Account, WorkspaceId: Workspace, MessageId: Guid.NewGuid(), SpaceId: Space,
        Sequence: 1234567, AuthorId: Author, Kind: "user", Content: content, ClientMessageId: Guid.NewGuid().ToString("N"),
        Version: 1, CorrelationId: Guid.NewGuid(), ActorUserId: Author, OccurredAt: Now, ReplyToId: Guid.NewGuid(),
        ThreadRootId: threadRoot, ThreadSequence: threadRoot is null ? null : 12);

    private static RealtimeNotification Notification(RealtimeResourceChangedV1 change) => new(
        EventId: change.EventId.ToString("N"), EventType: change.ChangeKind, ChangeKind: change.ChangeKind,
        StreamKey: change.StreamKey, StreamVersion: 1234567, ResourceKind: change.ResourceKind, ResourceId: change.ResourceId,
        WorkspaceId: Workspace, CorrelationId: change.CorrelationId.ToString("N"), OccurredAt: change.OccurredAt,
        ResourceVersion: change.StreamVersion, SchemaVersion: 1, PayloadContract: change.PayloadContract, Payload: change.Payload);

    private int Measure(string name, RealtimeResourceChangedV1 change)
    {
        var frame = RealtimeProtocol.Event(SubscriptionId, Notification(change));
        output.WriteLine($"{name,-34} {frame.Length,5} bytes (payload {Encoding.UTF8.GetByteCount(change.Payload.GetRawText())})");
        JsonDocument.Parse(frame).RootElement.TryGetProperty("payloadOmitted", out _).Should().BeFalse(name + " keeps its payload");
        return frame.Length;
    }

    [Fact]
    public void Every_chat_frame_is_under_two_kilobytes()
    {
        var root = Guid.NewGuid();
        var sizes = new Dictionary<string, int>
        {
            ["message created"] = Measure("message created", ChatRealtime.ForCreated(Created("Short message, a sentence or two."), Now)),
            ["thread reply"] = Measure("thread reply", ChatRealtime.ForCreated(Created("A reply in a thread.", root), Now)),
            ["message edited"] = Measure("message edited", ChatRealtime.ForUpdated(new ChatMessageUpdatedIntegrationEvent(
                EventId: Guid.NewGuid(), AccountId: Account, WorkspaceId: Workspace, MessageId: Guid.NewGuid(), SpaceId: Space,
                Sequence: 1234567, AuthorId: Author, Content: "Edited text of the message.", Version: 2,
                CorrelationId: Guid.NewGuid(), ActorUserId: Author, OccurredAt: Now), Now)),
            ["message deleted"] = Measure("message deleted", ChatRealtime.ForDeleted(new ChatMessageDeletedIntegrationEvent(
                EventId: Guid.NewGuid(), AccountId: Account, WorkspaceId: Workspace, MessageId: Guid.NewGuid(), SpaceId: Space,
                Sequence: 1234567, AuthorId: Author, DeletedBy: Author, Version: 3, CorrelationId: Guid.NewGuid(),
                ActorUserId: Author, OccurredAt: Now), Now)),
            ["reaction"] = Measure("reaction", ChatRealtime.ForReactionChanged(new ChatReactionChangedIntegrationEvent(
                Guid.NewGuid(), Account, Workspace, Space, Guid.NewGuid(), "thumbs_up", "added", Guid.NewGuid(), Author, null, Now), Now)),
            ["pin"] = Measure("pin", ChatRealtime.ForSpaceChanged(new ChatSpaceChangedIntegrationEvent(
                Guid.NewGuid(), Account, Workspace, Space, Guid.NewGuid(), "pinned", Guid.NewGuid(), Author, null, Now), Now)),
            ["unread with preview"] = Measure("unread with preview", ChatRealtime.Unread(
                Account, Workspace, Author, Space, 42, 3, Guid.NewGuid(), Guid.NewGuid(), Now, 1234567,
                new ChatRealtime.UnreadPreview(1234567, Author, "user", ChatRealtime.PreviewText(new string('x', 400)), Now))),
            ["read receipt"] = Measure("read receipt", ChatRealtime.Unread(
                Account, Workspace, Author, Space, 0, 0, Guid.NewGuid(), Guid.NewGuid(), Now, 1234567)),
        };

        sizes.Should().AllSatisfy(s => s.Value.Should().BeLessThan(Budget, s.Key));
    }

    [Theory]
    [InlineData("a", 5000)]
    [InlineData("é", 5000)]
    [InlineData("中", 5000)]
    [InlineData("\U0001F600", 2500)]
    [InlineData("a+\"\\<>&", 800)]
    [InlineData("a", 880)]
    [InlineData("é", 600)]
    [InlineData("\U0001F600", 300)]
    [InlineData("a+", 500)]
    public void A_long_message_in_any_script_still_fits_or_is_left_out(string unit, int repeat)
    {
        var text = string.Concat(Enumerable.Repeat(unit, repeat));

        var created = Measure($"created {unit} x{repeat}", ChatRealtime.ForCreated(Created(text), Now));
        var thread = Measure($"thread reply {unit} x{repeat}", ChatRealtime.ForCreated(Created(text, Guid.NewGuid()), Now));

        created.Should().BeLessThan(Budget);
        thread.Should().BeLessThan(Budget);
    }

    [Fact]
    public void A_null_field_is_left_out_of_the_payload_and_a_plus_sign_is_not_escaped()
    {
        var change = ChatRealtime.ForCreated(Created("hello"), Now);
        var frame = Encoding.UTF8.GetString(RealtimeProtocol.Event(SubscriptionId, Notification(change)));

        change.Payload.TryGetProperty("editedAt", out _).Should().BeFalse();
        change.Payload.TryGetProperty("threadRootId", out _).Should().BeFalse();
        frame.Should().NotContain("\\u002B");
    }

    [Fact]
    public void The_ephemeral_frames_are_tiny()
    {
        var typing = RealtimeProtocol.Typing(SubscriptionId, Author, 5000).Length;
        var presence = RealtimeProtocol.Presence(SubscriptionId, Author, "online", 60000).Length;
        output.WriteLine($"typing {typing} bytes, presence {presence} bytes");

        typing.Should().BeLessThan(256);
        presence.Should().BeLessThan(256);
    }
}
