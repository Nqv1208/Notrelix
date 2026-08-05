namespace Notrelix.Domain.Identity.Tokens;

public abstract class OneTimeUseToken : AggregateRoot
{
    public Guid UserId { get; protected set; }
    public TokenHash TokenHash { get; protected set; } = null!;
    public int HashVersion { get; protected set; }
    public UserTokenStatus Status { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? ExpiredAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string? RevocationReason { get; protected set; }

    protected OneTimeUseToken() : base() { }

    protected void Initialize(
        Guid userId,
        TokenHash tokenHash,
        int hashVersion,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(tokenHash);

        if (hashVersion <= 0)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_OneTimeToken_HashVersionMustBePositive, "Token hash version must be positive.");

        if (expiresAt <= createdAt)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_OneTimeToken_ExpirationMustBeAfterCreation, "Token expiration time must be after creation time.");

        UserId = userId;
        TokenHash = tokenHash;
        HashVersion = hashVersion;
        ExpiresAt = expiresAt;
        Status = UserTokenStatus.Active;
    }

    public void MarkUsed(DateTimeOffset usedAt, DomainEvent? domainEvent = null)
    {
        if (Status == UserTokenStatus.Used)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_OneTimeToken_AlreadyUsed, "Token has already been used.");

        if (Status is UserTokenStatus.Expired or UserTokenStatus.Revoked || usedAt >= ExpiresAt)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_OneTimeToken_CannotUseExpired, "Cannot use an expired token.");

        var pending = PrepareAuditUpdate(UserId, usedAt);
        Status = UserTokenStatus.Used;
        UsedAt = usedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        if (domainEvent != null) RaiseDomainEvent(domainEvent);
    }

    public bool TryExpire(DateTimeOffset expiredAt, DomainEvent? domainEvent = null)
    {
        if (Status == UserTokenStatus.Expired) return false;

        if (Status == UserTokenStatus.Used)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_OneTimeToken_CannotExpireUsed, "Cannot expire a used token.");

        if (Status == UserTokenStatus.Revoked) return false;

        var pending = PrepareAuditUpdate(UserId, expiredAt);
        Status = UserTokenStatus.Expired;
        ExpiredAt = expiredAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        if (domainEvent != null) RaiseDomainEvent(domainEvent);
        return true;
    }

    public bool TryRevoke(
        DateTimeOffset revokedAt,
        string revocationReason,
        DomainEvent? domainEvent = null)
    {
        Guard.NotNullOrWhiteSpace(revocationReason);
        Guard.MaxLength(revocationReason, 256);

        if (Status == UserTokenStatus.Revoked)
            return false;

        if (Status is UserTokenStatus.Used or UserTokenStatus.Expired)
            return false;

        var pending = PrepareAuditUpdate(UserId, revokedAt);
        Status = UserTokenStatus.Revoked;
        RevokedAt = revokedAt;
        RevocationReason = revocationReason.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        if (domainEvent != null) RaiseDomainEvent(domainEvent);
        return true;
    }
}
