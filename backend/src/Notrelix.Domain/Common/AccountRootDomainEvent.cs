using Notrelix.Domain.Common.Constants;

namespace Notrelix.Domain.Common;

public abstract record AccountRootDomainEvent : AccountScopedDomainEvent
{
    protected AccountRootDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(accountId, occurredAt, actorUserId, correlationId, causationId, subjectId: accountId)
    {
        SourceContext = SourceContexts.Accounts;
        AggregateType = AggregateTypes.Account;
        AggregateId = accountId;
        SubjectType = SubjectTypes.Account;
    }
}