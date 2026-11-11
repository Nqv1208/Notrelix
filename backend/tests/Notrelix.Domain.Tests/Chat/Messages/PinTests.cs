using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>The pin rules (CHT-006, UC-CHT-F06, F07): a pin is of a live user message, the space holds at most the limit, and the end of a pin is a fact.</summary>
public class PinTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();

    private static Message UserMessage(string text = "hello") =>
        Message.CreateUserMessage(Account, Workspace, Space, 1, Author, text, $"c-{Guid.NewGuid():N}", null, Now);

    [Fact]
    public void Pinning_a_live_user_message_raises_a_pinned_fact()
    {
        var message = UserMessage();

        var pin = Pin.Create(Account, Workspace, Space, message.Id, Author, Now);

        pin.MessageId.Should().Be(message.Id);
        pin.SpaceId.Should().Be(Space);
        pin.PinnedBy.Should().Be(Author);
        pin.PinnedAt.Should().Be(Now);
        var fact = pin.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<MessagePinChangedDomainEvent>().Subject;
        fact.Pinned.Should().BeTrue();
        fact.SpaceId.Should().Be(Space);
    }

    [Fact]
    public void Removing_a_pin_raises_an_unpinned_fact_with_the_actor()
    {
        var pin = Pin.Create(Account, Workspace, Space, Guid.NewGuid(), Author, Now);
        var remover = Guid.NewGuid();

        pin.Remove(remover, Now.AddMinutes(5));

        pin.DomainEvents.OfType<MessagePinChangedDomainEvent>().Should().HaveCount(2);
        var fact = pin.DomainEvents.OfType<MessagePinChangedDomainEvent>().Last();
        fact.Pinned.Should().BeFalse();
        fact.ActorId.Should().Be(remover);
    }

    [Fact]
    public void A_system_message_cannot_be_pinned()
    {
        var system = Message.CreateSystemMessage(Account, Workspace, Space, 2, Author, "joined the channel", Now);

        var act = () => system.EnsurePinnable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotPinnable);
    }

    [Fact]
    public void A_deleted_message_cannot_be_pinned()
    {
        var message = UserMessage();
        message.Delete(Author, Now.AddMinutes(1));

        var act = () => message.EnsurePinnable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
    }

    [Fact]
    public void A_space_with_fewer_than_the_limit_has_room_and_a_full_one_does_not()
    {
        var room = () => Pin.EnsureRoom(ChatLimits.PinLimit - 1);
        room.Should().NotThrow();

        var full = () => Pin.EnsureRoom(ChatLimits.PinLimit);
        full.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void The_pin_limit_is_fifty_per_space()
    {
        ChatLimits.PinLimit.Should().Be(50);
    }
}
