using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Attachments;

public record AttachmentCreatedEvent(Guid AttachmentId, ResourceRef Target) : DomainRecordEvent;
public record AttachmentDeletedEvent(Guid AttachmentId) : DomainRecordEvent;
