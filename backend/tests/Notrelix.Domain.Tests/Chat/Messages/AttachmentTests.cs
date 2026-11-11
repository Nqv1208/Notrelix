using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>The rule for a file on a message (CHT-012, UC-CHT-G08): a live message of a person takes one; a system message or a tombstone does not.</summary>
public class AttachmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid Space = Guid.NewGuid();
    private static readonly Guid Author = Guid.NewGuid();

    private static Message UserMessage() =>
        Message.CreateUserMessage(Account, Workspace, Space, 1, Author, "hello", $"c-{Guid.NewGuid():N}", null, Now);

    [Fact]
    public void A_live_message_of_a_person_takes_a_file()
    {
        var act = () => UserMessage().EnsureAttachable();

        act.Should().NotThrow();
    }

    [Fact]
    public void A_system_message_takes_no_file()
    {
        var system = Message.CreateSystemMessage(Account, Workspace, Space, 2, Author, "A pin was added.", Now);

        var act = () => system.EnsureAttachable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotAttachable);
    }

    [Fact]
    public void A_tombstone_takes_no_file()
    {
        var message = UserMessage();
        message.Delete(Author, Now.AddMinutes(1));

        var act = () => message.EnsureAttachable();

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
    }

    [Fact]
    public void The_target_kind_of_a_message_file_is_chat_message()
    {
        Notrelix.Domain.SharedKernel.ResourceKind.Create("chat.message").Value.Should().Be("chat.message");
    }
}
