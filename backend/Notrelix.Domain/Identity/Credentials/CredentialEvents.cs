using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Credentials;

public record PasswordResetRequestedEvent(Guid UserId, string Email) : DomainRecordEvent;
public record PasswordResetCompletedEvent(Guid UserId) : DomainRecordEvent;
public record EmailVerificationRequestedEvent(Guid UserId, string Email) : DomainRecordEvent;
public record EmailVerificationCompletedEvent(Guid UserId) : DomainRecordEvent;
