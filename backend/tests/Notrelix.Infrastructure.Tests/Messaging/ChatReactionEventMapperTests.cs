using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>The reaction fact reaches the outbox as chat.reaction.changed, added or removed, on the space's workspace (UC-CHT-G06, G07).</summary>
public sealed class ChatReactionEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_added_reaction_maps_to_an_added_change_on_the_space()
    {
        var fact = new MessageReactionChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "👍", Added: true, Now);

        var mapped = new ChatReactionEventMapper().Map(fact);

        mapped.Should().BeOfType<ChatReactionChangedIntegrationEvent>();
        mapped!.ChangeKind.Should().Be("added");
        mapped.Kind.Should().Be("👍");
        mapped.SpaceId.Should().Be(fact.SpaceId);
        mapped.MessageId.Should().Be(fact.MessageId);
        mapped.WorkspaceId.Should().Be(fact.WorkspaceId);
        mapped.ActorUserId.Should().Be(fact.ActorId);
    }

    [Fact]
    public void A_removed_reaction_maps_to_a_removed_change()
    {
        var fact = new MessageReactionChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "🚀", Added: false, Now);

        new ChatReactionEventMapper().Map(fact)!.ChangeKind.Should().Be("removed");
    }

    [Fact]
    public void Through_the_domain_interface_it_returns_one_mapping_for_a_reaction_fact()
    {
        var fact = new MessageReactionChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "🎉", Added: true, Now);
        var mapper = (IIntegrationEventMapper)new ChatReactionEventMapper();

        mapper.Map(fact).Should().ContainSingle();
    }
}
