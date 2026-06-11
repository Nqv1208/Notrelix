using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public record MfaEnabledEvent(Guid UserId, MfaMethodType Type) : DomainRecordEvent;
public record MfaDisabledEvent(Guid UserId, MfaMethodType Type) : DomainRecordEvent;
public record LoginAttemptRecordedEvent(Guid UserId, string? Email, bool Succeeded) : DomainRecordEvent;
