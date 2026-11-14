using Notrelix.Application.EventMappers.Chat;
using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Infrastructure.Tests.Messaging;

/// <summary>The message attachment fact reaches the outbox as chat.message.attachment.changed, added or removed, on the space's workspace (UC-CHT-G08).</summary>
public sealed class ChatMessageAttachmentEventMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_added_file_maps_with_its_metadata()
    {
        var uploader = Guid.NewGuid();
        var fact = new MessageAttachmentChangedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), uploader, Added: true, Guid.NewGuid(), "a.pdf", 10, "application/pdf", uploader, Now, Now);

        var mapped = new ChatMessageAttachmentEventMapper().Map(fact);

        mapped.Should().BeOfType<ChatMessageAttachmentChangedIntegrationEvent>();
        mapped!.ChangeKind.Should().Be("added");
        mapped.FileName.Should().Be("a.pdf");
        mapped.SizeBytes.Should().Be(10);
        mapped.AttachmentId.Should().Be(fact.AttachmentId);
        mapped.SpaceId.Should().Be(fact.SpaceId);
        mapped.MessageId.Should().Be(fact.MessageId);
        mapped.WorkspaceId.Should().Be(fact.WorkspaceId);
        mapped.ActorUserId.Should().Be(uploader);
    }

    [Fact]
    public void A_removed_file_maps_to_a_removed_change_without_metadata()
    {
        var fact = new MessageAttachmentChangedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Added: false, Guid.NewGuid(), null, null, null, null, null, Now);

        var mapped = new ChatMessageAttachmentEventMapper().Map(fact);

        mapped!.ChangeKind.Should().Be("removed");
        mapped.FileName.Should().BeNull();
    }

    [Fact]
    public void Through_the_domain_interface_it_returns_one_mapping()
    {
        var fact = new MessageAttachmentChangedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Added: false, Guid.NewGuid(), null, null, null, null, null, Now);

        ((IIntegrationEventMapper)new ChatMessageAttachmentEventMapper()).Map(fact).Should().ContainSingle();
    }
}
