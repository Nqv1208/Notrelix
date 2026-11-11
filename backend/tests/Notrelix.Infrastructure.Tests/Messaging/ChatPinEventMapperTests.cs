using Notrelix.Application.EventMappers;
using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>The pin fact reaches the outbox as chat.space.changed, pinned or unpinned, on the space's workspace (UC-CHT-F06, F07).</summary>
public sealed class ChatPinEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_pin_maps_to_a_pinned_change_on_the_space()
    {
        var fact = new MessagePinChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Pinned: true, Now);

        var mapped = new ChatPinEventMapper().Map(fact);

        mapped.Should().BeOfType<ChatSpaceChangedIntegrationEvent>();
        mapped!.ChangeKind.Should().Be("pinned");
        mapped.SpaceId.Should().Be(fact.SpaceId);
        mapped.MessageId.Should().Be(fact.MessageId);
        mapped.WorkspaceId.Should().Be(fact.WorkspaceId);
    }

    [Fact]
    public void An_unpin_maps_to_an_unpinned_change()
    {
        var fact = new MessagePinChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Pinned: false, Now);

        new ChatPinEventMapper().Map(fact)!.ChangeKind.Should().Be("unpinned");
    }

    [Fact]
    public void Through_the_domain_interface_it_returns_one_mapping_for_a_pin_fact()
    {
        var fact = new MessagePinChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Pinned: true, Now);
        var mapper = (IIntegrationEventMapper)new ChatPinEventMapper();

        mapper.Map(fact).Should().ContainSingle();
    }
}
