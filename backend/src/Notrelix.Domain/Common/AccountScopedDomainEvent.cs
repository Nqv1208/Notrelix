namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : DomainEvent
{
    public Guid AccountId { get; }

    protected AccountScopedDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null,
        Guid subjectId = default)
        : base(occurredAt, workspaceId: null, actorUserId, subjectId)
    {
        AccountId = accountId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}