using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions;

public record UserSessionCreatedEvent(Guid SessionId, Guid UserId) : DomainRecordEvent;
public record UserSessionRevokedEvent(Guid SessionId, Guid UserId) : DomainRecordEvent;
public record UserSessionExpiredEvent(Guid SessionId, Guid UserId) : DomainRecordEvent;
