using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Realtime;

namespace Notrelix.Application.Tests.Features.Chat.Realtime;

/// <summary>UC-CHT-F06, F07 at the level of the realtime fact: a pin change is a notify-only frame on the channel stream.</summary>
public class ChatSpaceChangedRealtimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid SpaceId = Guid.NewGuid();

    private static ChatSpaceChangedIntegrationEvent Changed(string kind) => new(
        EventId: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        WorkspaceId: Guid.NewGuid(),
        SpaceId: SpaceId,
        MessageId: Guid.NewGuid(),
        ChangeKind: kind,
        CorrelationId: Guid.NewGuid(),
        ActorUserId: Guid.NewGuid(),
        OccurredAt: Now);

    [Fact]
    public void A_pin_change_is_a_channel_frame_named_chat_space_changed_with_the_change_and_the_message()
    {
        var fact = Changed("pinned");
        var change = ChatRealtime.ForSpaceChanged(fact, Now);

        change.TopicNamespace.Should().Be("chat");
        change.ResourceKind.Should().Be("Channel");
        change.ResourceId.Should().Be(SpaceId);
        change.StreamKey.Should().Be($"chat:Channel:{SpaceId:N}");
        change.ChangeKind.Should().Be(ChatRealtime.SpaceChangedEvent);
        change.PayloadContract.Should().Be("chat.space.v1");
        change.Payload.GetProperty("change").GetString().Should().Be("pinned");
        change.Payload.GetProperty("messageId").GetGuid().Should().Be(fact.MessageId);
        change.Payload.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["change", "messageId"], "no content travels in a pin frame");
    }

    [Fact]
    public void An_unpin_change_is_the_same_frame_name_with_the_unpinned_change()
    {
        var unpinned = ChatRealtime.ForSpaceChanged(Changed("unpinned"), Now);

        unpinned.ChangeKind.Should().Be(ChatRealtime.SpaceChangedEvent);
        unpinned.PayloadContract.Should().Be("chat.space.v1");
        unpinned.Payload.GetProperty("change").GetString().Should().Be("unpinned");
    }

    [Fact]
    public void A_message_frame_keeps_its_own_payload_contract()
    {
        var quoted = ChatRealtime.ForSpaceChanged(Changed("pinned"), Now);

        quoted.PayloadContract.Should().NotBe(ChatRealtime.MessagePayloadContract);
    }
}
