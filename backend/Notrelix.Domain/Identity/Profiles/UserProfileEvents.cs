using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Profiles;

public record UserProfileUpdatedEvent(Guid UserId) : DomainRecordEvent;
