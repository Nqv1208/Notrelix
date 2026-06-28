namespace Notrelix.Domain.Identity.Tokens;

public abstract class OneTimeUseToken : AggregateRoot
{
    public Guid UserId { get; protected set; }
    public TokenHash TokenHash { get; protected set; } = null!;
    public UserTokenStatus Status { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? ExpiredAt { get; protected set; }

    protected OneTimeUseToken() : base() { }

    protected void Initialize(Guid userId, TokenHash tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(tokenHash);

        if (expiresAt <= createdAt)
            throw new BusinessRuleException("Token expiration time must be after creation time.");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        Status = UserTokenStatus.Active;
    }

    public void MarkUsed(DateTimeOffset usedAt, DomainEvent? domainEvent = null)
    {
        EnsureNotDeleted();

        if (Status == UserTokenStatus.Used)
            throw new BusinessRuleException("Token has already been used.");

        if (Status == UserTokenStatus.Expired || usedAt >= ExpiresAt)
            throw new BusinessRuleException("Cannot use an expired token.");

        Status = UserTokenStatus.Used;
        UsedAt = usedAt;
        SetAuditOnUpdate(UserId, usedAt);
        IncrementVersion();
        if (domainEvent != null) AddDomainEvent(domainEvent);
    }

    public bool TryExpire(DateTimeOffset expiredAt, DomainEvent? domainEvent = null)
    {
        EnsureNotDeleted();

        if (Status == UserTokenStatus.Expired) return false;

        if (Status == UserTokenStatus.Used)
            throw new BusinessRuleException("Cannot expire a used token.");

        Status = UserTokenStatus.Expired;
        ExpiredAt = expiredAt;
        SetAuditOnUpdate(UserId, expiredAt);
        IncrementVersion();
        if (domainEvent != null) AddDomainEvent(domainEvent);
        return true;
    }
}
