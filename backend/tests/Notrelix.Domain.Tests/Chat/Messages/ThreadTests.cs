using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>The thread rules (CHT-013, UC-CHT-F01, F05): one thread per root, a gap-free thread sequence, inactivity archive and reopen on reply.</summary>
public class MessageThreadTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private static MessageThread Open(DateTimeOffset? at = null) =>
        MessageThread.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), at ?? Now);

    [Fact]
    public void A_new_thread_has_no_replies_and_is_open()
    {
        var thread = Open();

        thread.ReplyCount.Should().Be(0);
        thread.ArchivedAt.Should().BeNull();
        thread.IsArchivedAt(Now).Should().BeFalse();
    }

    [Fact]
    public void Each_reply_takes_the_next_thread_sequence_gap_free()
    {
        var thread = Open();

        thread.RecordReply(Now).Should().Be(1);
        thread.RecordReply(Now.AddMinutes(1)).Should().Be(2);
        thread.RecordReply(Now.AddMinutes(2)).Should().Be(3);
        thread.ReplyCount.Should().Be(3);
        thread.LastReplyAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void A_thread_is_archived_after_seven_days_without_a_reply()
    {
        var thread = Open();

        thread.IsArchivedAt(Now.AddDays(ChatLimits.ThreadInactivityDays - 1)).Should().BeFalse();
        thread.IsArchivedAt(Now.AddDays(ChatLimits.ThreadInactivityDays)).Should().BeTrue();
    }

    [Fact]
    public void A_reply_reopens_an_archived_thread_and_resets_its_inactivity()
    {
        var thread = Open();
        thread.ArchivedAt.Should().BeNull();
        var later = Now.AddDays(10);
        thread.IsArchivedAt(later).Should().BeTrue("inactive for ten days");

        thread.RecordReply(later);

        thread.ArchivedAt.Should().BeNull();
        thread.IsArchivedAt(later).Should().BeFalse("a reply just happened");
    }

    [Fact]
    public void A_thread_needs_a_root_and_a_space()
    {
        var act = () => MessageThread.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Now);

        act.Should().Throw<BusinessRuleException>("a guard refuses an empty id");
    }
}

/// <summary>The follow state and read pointer of one person on one thread (UC-CHT-F04, F08).</summary>
public class ThreadFollowerTests
{
    private static ThreadFollower Follow(bool muted = false, long read = 0) =>
        ThreadFollower.Follow(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), muted, read);

    [Fact]
    public void A_follower_starts_unmuted_by_default_and_can_be_muted()
    {
        var follower = Follow();
        follower.Muted.Should().BeFalse();

        follower.SetMuted(true);

        follower.Muted.Should().BeTrue();
    }

    [Fact]
    public void Unread_replies_are_the_replies_above_the_read_pointer()
    {
        var follower = Follow(read: 2);

        follower.UnreadReplies(5).Should().Be(3);
        follower.UnreadReplies(2).Should().Be(0, "a pointer at the count leaves nothing unread");
        follower.UnreadReplies(1).Should().Be(0, "a pointer above the count never shows a negative");
    }

    [Fact]
    public void The_read_pointer_only_moves_forward()
    {
        var follower = Follow(read: 4);

        follower.MarkReadThrough(3);
        follower.ReadReplySequence.Should().Be(4);

        follower.MarkReadThrough(7);
        follower.ReadReplySequence.Should().Be(7);
    }

    [Fact]
    public void A_negative_starting_pointer_is_clamped_to_zero()
    {
        Follow(read: -5).ReadReplySequence.Should().Be(0);
    }
}

/// <summary>A thread reply is a message of the space with the thread's root and its own thread sequence (UC-CHT-F01).</summary>
public class ThreadReplyMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_reply_carries_the_root_its_thread_sequence_and_the_also_sent_flag()
    {
        var root = Guid.NewGuid();
        var reply = Message.CreateThreadReply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 9, root, 2, Guid.NewGuid(), "  hello  ", "c-1", alsoSentToChannel: true, Now);

        reply.IsThreadReply.Should().BeTrue();
        reply.ThreadRootId.Should().Be(root);
        reply.ThreadSequence.Should().Be(2);
        reply.AlsoSentToChannel.Should().BeTrue();
        reply.Sequence.Should().Be(9, "the space sequence is the one the store assigned");
        reply.Content.Should().Be("hello", "the text is trimmed like any message");
    }

    [Fact]
    public void A_reply_raises_the_created_fact_with_the_thread_identity()
    {
        var root = Guid.NewGuid();
        var reply = Message.CreateThreadReply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, root, 1, Guid.NewGuid(), "x", "c-2", false, Now);

        var fact = reply.DomainEvents.OfType<MessageCreatedDomainEvent>().Single();
        fact.ThreadRootId.Should().Be(root);
        fact.ThreadSequence.Should().Be(1);
    }

    [Fact]
    public void A_main_flow_message_is_not_a_thread_reply()
    {
        var message = Message.CreateUserMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "x", "c-3", null, Now);

        message.IsThreadReply.Should().BeFalse();
        message.ThreadSequence.Should().BeNull();
        message.AlsoSentToChannel.Should().BeFalse();
    }

    [Fact]
    public void A_thread_sequence_starts_at_one()
    {
        var act = () => Message.CreateThreadReply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, Guid.NewGuid(), 0, Guid.NewGuid(), "x", "c-4", false, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageSequenceInvalid);
    }
}
