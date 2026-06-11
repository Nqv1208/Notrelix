using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users;

public record UserLoggedInEvent(Guid UserId, DateTimeOffset LoggedInAt) : DomainRecordEvent;
public record UserRegisteredEvent(Guid UserId, string Email) : DomainRecordEvent;
