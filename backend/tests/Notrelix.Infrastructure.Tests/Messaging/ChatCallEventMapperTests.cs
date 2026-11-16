using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Calls;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>The call fact reaches the outbox as chat.call.changed with the wire spelling, the roster and the version (SPEC-CHAT-MEDIA-CALLS section 7).</summary>
public sealed class ChatCallEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_started_call_maps_to_a_ringing_change_that_rings_a_direct_conversation()
    {
        var starter = Guid.NewGuid();
        var call = Call.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SpaceKind.Direct, starter, CallKind.Video, "room", "ap-southeast-1", Now);
        var fact = call.DomainEvents.OfType<Notrelix.Domain.Chat.Calls.Events.CallChangedDomainEvent>().Single();

        var mapped = new ChatCallEventMapper().Map(fact);

        mapped.Should().BeOfType<ChatCallChangedIntegrationEvent>();
        mapped!.State.Should().Be("ringing");
        mapped.Kind.Should().Be("video");
        mapped.RingsMembers.Should().BeTrue();
        mapped.ParticipantIds.Should().Equal(starter);
        mapped.Version.Should().Be(1);
        mapped.SpaceId.Should().Be(call.SpaceId);
        mapped.CallId.Should().Be(call.Id);
        mapped.ActorUserId.Should().Be(starter);
    }

    [Fact]
    public void An_ended_call_carries_its_reason_and_an_empty_roster()
    {
        var starter = Guid.NewGuid();
        var call = Call.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SpaceKind.ChannelPublic, starter, CallKind.Voice, "room", "ap-southeast-1", Now);
        call.Cancel(starter, Now.AddSeconds(3));
        var fact = call.DomainEvents.OfType<Notrelix.Domain.Chat.Calls.Events.CallChangedDomainEvent>().Last();

        var changed = new ChatCallEventMapper().Map(fact)!;
        changed.State.Should().Be("ended");
        changed.EndReason.Should().Be("cancelled");
        changed.ParticipantIds.Should().BeEmpty();
        changed.RingsMembers.Should().BeFalse("a channel call shows a banner and does not ring");
    }
}
