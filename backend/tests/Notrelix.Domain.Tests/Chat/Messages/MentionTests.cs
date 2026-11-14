using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>The shape rules of a message's mentions (CHT-010, UC-CHT-G01, G03, G04): spans fit the text, do not overlap, and match their kind.</summary>
public class MentionTests
{
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly Guid Channel = Guid.NewGuid();

    private static void Refused(string text, params MentionSpan[] spans) =>
        FluentActions.Invoking(() => MentionSpans.Normalize(text, spans))
            .Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()
            .Which.RuleCode.Should().Be(ChatRuleCodes.MentionInvalid);

    [Fact]
    public void No_spans_is_no_mentions()
    {
        MentionSpans.Normalize("hello", []).Should().BeEmpty();
    }

    [Fact]
    public void Spans_are_ordered_by_position_and_moved_onto_the_trimmed_text()
    {
        var spans = MentionSpans.Normalize("  @here and @Ann", [
            new MentionSpan(MentionKind.User, 12, 4, Person),
            new MentionSpan(MentionKind.BroadcastHere, 2, 5, null),
        ]);

        spans.Select(s => (s.Kind, s.Start)).Should().Equal((MentionKind.BroadcastHere, 0), (MentionKind.User, 10));
    }

    [Theory]
    [InlineData("@channel", MentionKind.BroadcastChannel)]
    [InlineData("@CHANNEL", MentionKind.BroadcastChannel)]
    [InlineData("@here", MentionKind.BroadcastHere)]
    public void A_broadcast_covers_exactly_its_token(string text, MentionKind kind)
    {
        MentionSpans.Normalize(text, [new MentionSpan(kind, 0, text.Length, null)]).Should().ContainSingle();
    }

    [Fact]
    public void A_broadcast_over_other_text_is_refused()
    {
        Refused("@everyone", new MentionSpan(MentionKind.BroadcastChannel, 0, 9, null));
        Refused("@channel", new MentionSpan(MentionKind.BroadcastHere, 0, 8, null));
    }

    [Fact]
    public void A_broadcast_names_no_target()
    {
        Refused("@here", new MentionSpan(MentionKind.BroadcastHere, 0, 5, Person));
    }

    [Fact]
    public void A_person_and_a_channel_need_an_id_and_the_right_first_character()
    {
        Refused("@Ann", new MentionSpan(MentionKind.User, 0, 4, null));
        Refused("@Ann", new MentionSpan(MentionKind.User, 0, 4, Guid.Empty));
        Refused("Ann", new MentionSpan(MentionKind.User, 0, 3, Person));
        Refused("@", new MentionSpan(MentionKind.User, 0, 1, Person));
        Refused("@dev", new MentionSpan(MentionKind.ChannelLink, 0, 4, Channel));
        MentionSpans.Normalize("#dev", [new MentionSpan(MentionKind.ChannelLink, 0, 4, Channel)]).Should().ContainSingle();
    }

    [Fact]
    public void A_span_outside_the_text_or_overlapping_another_is_refused()
    {
        Refused("@Ann", new MentionSpan(MentionKind.User, 0, 9, Person));
        Refused("@Ann", new MentionSpan(MentionKind.User, -1, 4, Person));
        Refused("@Ann", new MentionSpan(MentionKind.User, 0, 0, Person));
        Refused("@Ann @Bo", new MentionSpan(MentionKind.User, 0, 4, Person), new MentionSpan(MentionKind.User, 3, 3, Channel));
    }

    [Fact]
    public void A_span_in_the_trimmed_whitespace_is_refused()
    {
        Refused("  @Ann", new MentionSpan(MentionKind.User, 0, 4, Person));
    }

    [Fact]
    public void More_than_the_limit_is_refused_with_its_own_code()
    {
        var spans = Enumerable.Range(0, ChatLimits.MentionsPerMessageMax + 1).Select(i => new MentionSpan(MentionKind.User, i * 3, 2, Person)).ToArray();

        FluentActions.Invoking(() => MentionSpans.Normalize(new string('x', 400), spans))
            .Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()
            .Which.RuleCode.Should().Be(ChatRuleCodes.MentionLimitExceeded);
    }

    [Fact]
    public void The_kind_spelling_is_stable_and_round_trips()
    {
        new[] { MentionKind.User, MentionKind.ChannelLink, MentionKind.BroadcastChannel, MentionKind.BroadcastHere }
            .Select(k => k.ToWire()).Should().Equal("user", "channel-link", "broadcast-channel", "broadcast-here");
        MentionKindWire.MentionKindFromWire("broadcast-here").Should().Be(MentionKind.BroadcastHere);
        MentionKindWire.MentionKindFromWire("team").Should().BeNull();
    }

    [Fact]
    public void A_stored_mention_keeps_its_span_and_ids()
    {
        var span = new MentionSpan(MentionKind.User, 3, 4, Person);
        var row = MessageMention.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), span);

        row.ToSpan().Should().Be(span);
    }
}
