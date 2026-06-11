using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Presence;

public record PresenceUpdatedEvent(Guid UserId, PresenceStatus Status) : DomainRecordEvent;
