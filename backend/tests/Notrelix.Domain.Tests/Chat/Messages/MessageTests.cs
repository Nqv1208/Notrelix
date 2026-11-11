using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Tests.Chat.Messages;

/// <summary>Message invariants (spec section 10, CHT-005 to CHT-007, UC-CHT-B04, B05, B09).</summary>
public class MessageTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _spaceId = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private Message NewUserMessage(string content = "hello", long sequence = 1, string clientMessageId = "c-1") =>
        Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, sequence, _author, content, clientMessageId, null, _now);

    private Message NewSystemMessage(string content = "renamed the channel to x") =>
        Message.CreateSystemMessage(_accountId, _workspaceId, _spaceId, 4, _author, content, _now);

    [Fact]
    public void CreateUserMessage_KeepsTheSequenceAndTrimsTheText()
    {
        var message = NewUserMessage("  hello there  ", sequence: 7);

        message.Sequence.Should().Be(7);
        message.Content.Should().Be("hello there");
        message.Kind.Should().Be(MessageKind.User);
        message.AuthorId.Should().Be(_author);
        message.ClientMessageId.Should().Be("c-1");
        message.Version.Should().Be(1);
        message.IsDeleted.Should().BeFalse();
        message.IsEdited.Should().BeFalse();
    }

    [Fact]
    public void CreateUserMessage_RaisesMessageCreatedWithTheSequence()
    {
        var message = NewUserMessage(sequence: 3);

        var created = message.DomainEvents.OfType<MessageCreatedDomainEvent>().Should().ContainSingle().Subject;
        created.SpaceId.Should().Be(_spaceId);
        created.Sequence.Should().Be(3);
        created.AuthorId.Should().Be(_author);
        created.Kind.Should().Be(MessageKind.User);
        created.Content.Should().Be("hello");
        created.MessageId.Should().Be(message.Id);
    }

    [Fact]
    public void CreateUserMessage_AcceptsExactlyTheLimit()
    {
        var text = new string('a', ChatLimits.MessageMaxLength);

        var act = () => NewUserMessage(text);

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateUserMessage_CountsTheLimitAfterTrim()
    {
        var text = "  " + new string('a', ChatLimits.MessageMaxLength) + "  ";

        var act = () => NewUserMessage(text);

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateUserMessage_RefusesMoreThanTheLimitAfterTrim()
    {
        var text = new string('a', ChatLimits.MessageMaxLength + 1);

        var act = () => NewUserMessage(text);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageContentInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateUserMessage_RefusesBlankText(string text)
    {
        var act = () => NewUserMessage(text);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateUserMessage_RefusesASequenceBelowOne()
    {
        var act = () => NewUserMessage(sequence: 0);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageSequenceInvalid);
    }

    [Fact]
    public void CreateUserMessage_RefusesAClientMessageIdOverTheLimit()
    {
        var act = () => NewUserMessage(clientMessageId: new string('x', ChatLimits.ClientMessageIdMaxLength + 1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageContentInvalid);
    }

    [Fact]
    public void Edit_ByTheAuthor_ReplacesTheTextMarksItEditedAndKeepsTheSequence()
    {
        var message = NewUserMessage("first", sequence: 5);
        var later = _now.AddMinutes(2);

        var changed = message.Edit("second", _author, later);

        changed.Should().BeTrue();
        message.Content.Should().Be("second");
        message.IsEdited.Should().BeTrue();
        message.EditedAt.Should().Be(later);
        message.Sequence.Should().Be(5);
        message.Version.Should().Be(2);
        message.DomainEvents.OfType<MessageUpdatedDomainEvent>().Should().ContainSingle()
            .Which.Content.Should().Be("second");
    }

    [Fact]
    public void Edit_WithTheSameText_ChangesNothing()
    {
        var message = NewUserMessage("same");

        var changed = message.Edit("  same ", _author, _now.AddMinutes(1));

        changed.Should().BeFalse();
        message.IsEdited.Should().BeFalse();
        message.Version.Should().Be(1);
    }

    [Fact]
    public void Edit_ByAnotherPerson_IsRefused()
    {
        var message = NewUserMessage();

        var act = () => message.Edit("hijack", _other, _now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotAuthor);
    }

    [Fact]
    public void Edit_OfADeletedMessage_IsRefused()
    {
        var message = NewUserMessage();
        message.Delete(_author, _now.AddMinutes(1));

        var act = () => message.Edit("too late", _author, _now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
    }

    [Fact]
    public void Edit_OfASystemMessage_IsRefusedEvenForItsActor()
    {
        var message = NewSystemMessage();

        var act = () => message.Edit("renamed", _author, _now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
    }

    [Fact]
    public void Delete_ByTheAuthor_LeavesATombstoneWithAuthorTimeAndSequence()
    {
        var message = NewUserMessage("secret", sequence: 9);
        var deletedAt = _now.AddMinutes(3);

        var changed = message.Delete(_author, deletedAt);

        changed.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.Content.Should().BeNull();
        message.DeletedAt.Should().Be(deletedAt);
        message.DeletedBy.Should().Be(_author);
        message.AuthorId.Should().Be(_author);
        message.Sequence.Should().Be(9);
        message.CreatedAt.Should().Be(_now);
        message.Version.Should().Be(2);
        message.DomainEvents.OfType<MessageDeletedDomainEvent>().Should().ContainSingle()
            .Which.Sequence.Should().Be(9);
    }

    [Fact]
    public void Delete_OfAnAlreadyDeletedMessage_ChangesNothing()
    {
        var message = NewUserMessage();
        message.Delete(_author, _now.AddMinutes(1));

        var changed = message.Delete(_author, _now.AddMinutes(2));

        changed.Should().BeFalse();
        message.Version.Should().Be(2);
    }

    [Fact]
    public void Delete_ByAnotherPerson_IsRefused()
    {
        var message = NewUserMessage();

        var act = () => message.Delete(_other, _now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotAuthor);
    }

    [Fact]
    public void Delete_OfASystemMessage_IsRefused()
    {
        var message = NewSystemMessage();

        var act = () => message.Delete(_author, _now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
    }

    [Fact]
    public void CreateSystemMessage_IsAttributedToTheActorAndHasNoClientMessageId()
    {
        var message = NewSystemMessage("archived the channel");

        message.Kind.Should().Be(MessageKind.System);
        message.IsSystem.Should().BeTrue();
        message.AuthorId.Should().Be(_author);
        message.ClientMessageId.Should().BeNull();
        message.DomainEvents.OfType<MessageCreatedDomainEvent>().Should().ContainSingle()
            .Which.Kind.Should().Be(MessageKind.System);
    }
}
