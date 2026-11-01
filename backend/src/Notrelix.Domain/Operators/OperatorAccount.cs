namespace Notrelix.Domain.Operators;

/// <summary>
/// A person who operates the platform (not a tenant user): they sign in to the operator console with a
/// password, outside every tenant session. The row holds no tenant data and no tenant membership.
/// </summary>
public class OperatorAccount : AggregateRoot
{
    public const int MaxFailedSignIns = 5;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public OperatorStatus Status { get; private set; }
    public int FailedSignInCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset? LastSignedInAt { get; private set; }

    private OperatorAccount() : base() { }

    public static OperatorAccount Create(string email, string displayName, string passwordHash, DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNullOrWhiteSpace(displayName);
        Guard.NotNullOrWhiteSpace(passwordHash);
        Guard.MaxLength(email, 320);
        Guard.MaxLength(displayName, 160);

        var normalized = email.Trim().ToLowerInvariant();
        if (!normalized.Contains('@') || normalized.StartsWith('@') || normalized.EndsWith('@'))
            throw new BusinessRuleException(OperatorRuleCodes.Operators_Operator_EmailInvalid, "The operator email is not valid.");

        var account = new OperatorAccount
        {
            Email = normalized,
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            Status = OperatorStatus.Active
        };
        account.SetAuditOnCreate(null, createdAt);
        return account;
    }

    public bool IsLockedAt(DateTimeOffset now) => LockedUntil is { } until && until > now;

    /// <summary>A sign-in can be tried only by an active operator who is not locked out.</summary>
    public bool CanAttemptSignIn(DateTimeOffset now) => Status == OperatorStatus.Active && !IsLockedAt(now);

    public void RecordFailedSignIn(DateTimeOffset now)
    {
        // A lock that has run out starts a fresh count.
        if (LockedUntil is { } until && until <= now)
        {
            FailedSignInCount = 0;
            LockedUntil = null;
        }

        FailedSignInCount++;
        if (FailedSignInCount >= MaxFailedSignIns)
            LockedUntil = now.Add(LockDuration);

        var pending = PrepareAuditUpdate(null, now);
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void RecordSignedIn(DateTimeOffset now)
    {
        if (Status != OperatorStatus.Active)
            throw new BusinessRuleException(OperatorRuleCodes.Operators_Operator_Disabled, "The operator account is disabled.");

        var pending = PrepareAuditUpdate(null, now);
        FailedSignInCount = 0;
        LockedUntil = null;
        LastSignedInAt = now;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }
}
