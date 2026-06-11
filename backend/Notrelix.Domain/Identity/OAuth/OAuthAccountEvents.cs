using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth;

public record OAuthAccountLinkedEvent(Guid UserId, OAuthProvider Provider) : DomainRecordEvent;
public record OAuthAccountUnlinkedEvent(Guid UserId, OAuthProvider Provider) : DomainRecordEvent;
