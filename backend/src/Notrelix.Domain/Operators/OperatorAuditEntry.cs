namespace Notrelix.Domain.Operators;

/// <summary>
/// One operator action: who did it, to what, why, and the state before and after. Append-only: the entry
/// has no mutator, and the database grants the operator login insert and select only. It never carries
/// tenant content, only the operated record's identity and the changed values (a plan, a status, a limit).
/// </summary>
public class OperatorAuditEntry : Entity
{
    public const int MaxReasonLength = 500;

    public Guid OperatorId { get; private set; }
    public string Action { get; private set; } = null!;
    public string TargetKind { get; private set; } = null!;
    public Guid TargetId { get; private set; }
    public Guid? AccountId { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private OperatorAuditEntry() : base() { }

    public static OperatorAuditEntry Record(
        Guid operatorId,
        string action,
        string targetKind,
        Guid targetId,
        Guid? accountId,
        string reason,
        string? beforeJson,
        string? afterJson,
        DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(operatorId);
        Guard.NotEmpty(targetId);
        Guard.NotNullOrWhiteSpace(action);
        Guard.NotNullOrWhiteSpace(targetKind);

        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new BusinessRuleException(OperatorRuleCodes.Operators_Audit_ReasonRequired, "A reason is required.");
        if (trimmed.Length > MaxReasonLength)
            throw new BusinessRuleException(OperatorRuleCodes.Operators_Audit_ReasonTooLong, $"The reason is at most {MaxReasonLength} characters.");

        return new OperatorAuditEntry
        {
            OperatorId = operatorId,
            Action = action,
            TargetKind = targetKind,
            TargetId = targetId,
            AccountId = accountId,
            Reason = trimmed,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            OccurredAt = occurredAt
        };
    }
}
