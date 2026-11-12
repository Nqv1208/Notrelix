using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>Maps the message attachment fact to the chat.message.attachment.changed integration event (outbox, same transaction as the file change).</summary>
public sealed class ChatMessageAttachmentEventMapper : IntegrationEventMapperBase<MessageAttachmentChangedDomainEvent, ChatMessageAttachmentChangedIntegrationEvent>
{
    public const string AddedChange = "added";
    public const string RemovedChange = "removed";

    public override ChatMessageAttachmentChangedIntegrationEvent? Map(MessageAttachmentChangedDomainEvent domainEvent)
    {
        return new ChatMessageAttachmentChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: domainEvent.MessageId,
            ChangeKind: domainEvent.Added ? AddedChange : RemovedChange,
            AttachmentId: domainEvent.AttachmentId,
            FileName: domainEvent.FileName,
            SizeBytes: domainEvent.SizeBytes,
            ContentType: domainEvent.ContentType,
            UploadedBy: domainEvent.UploadedBy,
            AttachmentCreatedAt: domainEvent.AttachmentCreatedAt,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
