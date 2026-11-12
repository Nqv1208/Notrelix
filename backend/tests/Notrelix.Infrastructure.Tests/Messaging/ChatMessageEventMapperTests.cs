using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>
/// The Chat message mapper turns each Message fact into its public chat.message.* integration event, keeps the
/// tenant envelope, the sequence and the wire kind, and maps the content only for a live message.
/// </summary>
public sealed class ChatMessageEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MessageCreated_MapsToChatMessageCreatedV1WithTheSequenceAndKind()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var spaceId = Guid.CreateVersion7();
        var authorId = Guid.CreateVersion7();
        var domainEvent = new MessageCreatedDomainEvent(
            accountId, workspaceId, messageId, spaceId, 12, authorId, MessageKind.User, "hello", "client-12", 1, Now);

        var mapped = new ChatMessageEventMapper().Map(domainEvent);

        mapped.Should().NotBeNull();
        mapped!.AccountId.Should().Be(accountId);
        mapped.WorkspaceId.Should().Be(workspaceId);
        mapped.MessageId.Should().Be(messageId);
        mapped.SpaceId.Should().Be(spaceId);
        mapped.Sequence.Should().Be(12);
        mapped.AuthorId.Should().Be(authorId);
        mapped.Kind.Should().Be("user");
        mapped.Content.Should().Be("hello");
        mapped.ClientMessageId.Should().Be("client-12");
        mapped.Version.Should().Be(1);
        mapped.OccurredAt.Should().Be(Now);
    }

    [Fact]
    public void MessageCreated_OfASystemMessage_CarriesTheSystemKind()
    {
        var domainEvent = new MessageCreatedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 1,
            Guid.CreateVersion7(), MessageKind.System, "joined the channel", null, 1, Now);

        var mapped = new ChatMessageEventMapper().Map(domainEvent);

        mapped!.Kind.Should().Be("system");
        mapped.ClientMessageId.Should().BeNull();
    }

    [Fact]
    public void MessageUpdated_MapsToChatMessageUpdatedV1WithTheNewContent()
    {
        var domainEvent = new MessageUpdatedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 3,
            Guid.CreateVersion7(), "edited", 2, Now);

        var mapped = new ChatMessageEventMapper().Map(domainEvent);

        mapped.Should().NotBeNull();
        mapped!.Content.Should().Be("edited");
        mapped.Sequence.Should().Be(3);
        mapped.Version.Should().Be(2);
    }

    [Fact]
    public void MessageDeleted_MapsToChatMessageDeletedV1AndCarriesNoContent()
    {
        var deletedBy = Guid.CreateVersion7();
        var domainEvent = new MessageDeletedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 4,
            Guid.CreateVersion7(), deletedBy, 2, Now);

        var mapped = new ChatMessageEventMapper().Map(domainEvent);

        mapped.Should().BeOfType<ChatMessageDeletedIntegrationEvent>();
        mapped!.DeletedBy.Should().Be(deletedBy);
        mapped.Sequence.Should().Be(4);
    }

    [Fact]
    public void Map_ThroughTheDomainEventInterface_ReturnsOneMappingPerKnownFact()
    {
        var mapper = (IIntegrationEventMapper)new ChatMessageEventMapper();
        var created = new MessageCreatedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 1,
            Guid.CreateVersion7(), MessageKind.User, "x", "c", 1, Now);

        mapper.Map(created).Should().ContainSingle()
            .Which.IntegrationEvent.Should().BeOfType<ChatMessageCreatedIntegrationEvent>();
    }
}
