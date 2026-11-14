using System.Text;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Application.Tests.Features.Chat.Realtime;

/// <summary>
/// UC-CHT-G06, G07 at the level of the realtime fact: a reaction change is a compact frame on the channel stream of its space. The
/// frame carries the message, the kind, the change and the person; it carries no content and no counts. Its size stays under the
/// 4096-byte frame limit with the longest kind, and the reader refetches the summary over REST.
/// </summary>
public class ChatReactionRealtimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid SpaceId = Guid.NewGuid();

    private static ChatReactionChangedIntegrationEvent Changed(string change, string kind = "👍") => new(
        EventId: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        WorkspaceId: Guid.NewGuid(),
        SpaceId: SpaceId,
        MessageId: Guid.NewGuid(),
        Kind: kind,
        ChangeKind: change,
        CorrelationId: Guid.NewGuid(),
        ActorUserId: Guid.NewGuid(),
        OccurredAt: Now);

    [Fact]
    public void A_reaction_change_is_a_channel_frame_named_chat_reaction_changed()
    {
        var fact = Changed("added", "🎉");

        var change = ChatRealtime.ForReactionChanged(fact, Now);

        change.TopicNamespace.Should().Be("chat");
        change.ResourceKind.Should().Be("Channel");
        change.ResourceId.Should().Be(SpaceId);
        change.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");
        change.ChangeKind.Should().Be("chat.reaction.changed");
        change.PayloadContract.Should().Be("chat.reaction.v1");
        change.Payload.GetProperty("messageId").GetGuid().Should().Be(fact.MessageId);
        change.Payload.GetProperty("kind").GetString().Should().Be("🎉");
        change.Payload.GetProperty("change").GetString().Should().Be("added");
        change.Payload.GetProperty("actorId").GetGuid().Should().Be(fact.ActorUserId!.Value);
    }

    [Fact]
    public void The_payload_has_exactly_the_compact_fields_and_no_content_or_counts()
    {
        var change = ChatRealtime.ForReactionChanged(Changed("removed"), Now);

        change.Payload.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["messageId", "kind", "change", "actorId"]);
    }

    [Fact]
    public void A_removal_is_the_same_frame_name_with_the_removed_change()
    {
        var change = ChatRealtime.ForReactionChanged(Changed("removed"), Now);

        change.ChangeKind.Should().Be("chat.reaction.changed");
        change.Payload.GetProperty("change").GetString().Should().Be("removed");
    }

    [Fact]
    public void The_frame_stays_under_the_4096_byte_limit_with_the_longest_palette_kind()
    {
        var change = ChatRealtime.ForReactionChanged(Changed("added", "❤️"), Now);

        var bytes = Encoding.UTF8.GetByteCount(change.Payload.GetRawText());
        bytes.Should().BeLessThan(ChatRealtime.MaxPayloadBytes, "a reaction payload is far below the frame budget");
        bytes.Should().BeLessThan(4096);
    }

    [Fact]
    public void A_reaction_frame_does_not_reuse_the_message_or_pin_contracts()
    {
        var change = ChatRealtime.ForReactionChanged(Changed("added"), Now);

        change.PayloadContract.Should().NotBe(ChatRealtime.MessagePayloadContract);
        change.PayloadContract.Should().NotBe(ChatRealtime.SpaceChangedPayloadContract);
    }
}
