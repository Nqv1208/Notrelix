using Notrelix.Domain.Common.Constants;

namespace Notrelix.Domain.Common;

public abstract record AccountRootDomainEvent : AccountScopedDomainEvent
{
    protected AccountRootDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt)
        : base(accountId, occurredAt)
    {
        SourceContext = SourceContexts.Accounts;
        AggregateType = AggregateTypes.Account;
        AggregateId = accountId;
        SubjectType = SubjectTypes.Account;
    }
}