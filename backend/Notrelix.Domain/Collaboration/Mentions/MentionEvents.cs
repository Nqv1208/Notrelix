using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Mentions;

public record MentionCreatedEvent(Guid MentionId, ResourceRef Source, Guid MentionedId) : DomainRecordEvent;
