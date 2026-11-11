using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>
/// The reaction rules (CHT-011, UC-CHT-G06, G07): one reaction per message, person and kind; kinds are the six palette emoji; a
/// message holds at most the distinct and per-person limits; a system or deleted message takes no reaction; the end of a reaction is
/// a fact.
/// </summary>
public class ReactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Reactor = Guid.NewGuid();

    private static Message UserMessage(string text = "hello") =>
        Message.CreateUserMessage(Account, Workspace, Space, 1, Author, text, $"c-{Guid.NewGuid():N}", null, Now);

    [Fact]
    public void The_palette_is_the_six_collaboration_emoji_in_their_order()
    {
        ReactionKinds.Palette.Should().Equal("👍", "❤️", "😄", "🎉", "👀", "🚀");
    }

    [Theory]
    [InlineData("👍", true)]
    [InlineData("🚀", true)]
    [InlineData("🔥", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void A_kind_must_be_on_the_palette(string? kind, bool allowed)
    {
        ReactionKinds.Contains(kind).Should().Be(allowed);
    }

    [Fact]
    public void Reacting_with_a_palette_kind_raises_an_added_fact()
    {
        var reaction = Reaction.Create(Account, Workspace, Space, Guid.NewGuid(), Reactor, "🎉", Now);

        reaction.Kind.Should().Be("🎉");
        reaction.UserId.Should().Be(Reactor);
        reaction.CreatedAt.Should().Be(Now);
        var fact = reaction.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<MessageReactionChangedDomainEvent>().Subject;
        fact.Added.Should().BeTrue();
        fact.ActorId.Should().Be(Reactor);
        fact.Kind.Should().Be("🎉");
    }

    [Fact]
    public void A_kind_off_the_palette_is_refused()
    {
        var act = () => Reaction.Create(Account, Workspace, Space, Guid.NewGuid(), Reactor, "🔥", Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ReactionKindInvalid);
    }

    [Fact]
    public void Removing_a_reaction_raises_a_removed_fact_with_the_actor()
    {
        var reaction = Reaction.Create(Account, Workspace, Space, Guid.NewGuid(), Reactor, "👀", Now);
        reaction.Remove(Reactor, Now.AddMinutes(1));

        reaction.DomainEvents.Should().HaveCount(2);
        var fact = reaction.DomainEvents.Last().Should().BeOfType<MessageReactionChangedDomainEvent>().Subject;
        fact.Added.Should().BeFalse();
        fact.Kind.Should().Be("👀");
        fact.OccurredAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void A_message_holds_at_most_the_distinct_kinds_limit()
    {
        var act = () => Reaction.EnsureRoom(distinctKindsOnMessage: ChatLimits.ReactionKindsPerMessageMax, reactionsByActorOnMessage: 0, kindIsNew: true);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void A_kind_the_message_already_has_does_not_count_against_the_distinct_limit()
    {
        var act = () => Reaction.EnsureRoom(ChatLimits.ReactionKindsPerMessageMax, reactionsByActorOnMessage: 0, kindIsNew: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void A_person_holds_at_most_the_per_person_limit_on_one_message()
    {
        var act = () => Reaction.EnsureRoom(distinctKindsOnMessage: 1, reactionsByActorOnMessage: ChatLimits.ReactionsPerUserPerMessageMax, kindIsNew: false);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public void Below_both_limits_a_new_reaction_is_allowed()
    {
        var act = () => Reaction.EnsureRoom(distinctKindsOnMessage: ChatLimits.ReactionKindsPerMessageMax - 1, reactionsByActorOnMessage: 1, kindIsNew: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void A_live_user_message_takes_a_reaction()
    {
        var message = UserMessage();

        var act = () => message.EnsureReactable();

        act.Should().NotThrow();
    }

    [Fact]
    public void A_system_message_takes_no_reaction()
    {
        var system = Message.CreateSystemMessage(Account, Workspace, Space, 2, Author, "A pin was added.", Now);

        var act = () => system.EnsureReactable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotReactable);
    }

    [Fact]
    public void A_deleted_message_takes_no_reaction()
    {
        var message = UserMessage();
        message.Delete(Author, Now.AddMinutes(1));

        var act = () => message.EnsureReactable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
    }
}
