namespace Notrelix.Domain.Automation.Executions;

/// <summary>
/// UC-AUT-I04: why an execution failed or was retried, as a stable class. The class is the contract
/// (the stored value and the retry decision); the free-text error stays a human detail. Only
/// <see cref="Transient"/> may be retried; every other class ends the execution.
/// </summary>
public enum AutomationFailureClass
{
    /// <summary>The target (the rule, the item, the group or the board) no longer exists or is not visible. Terminal.</summary>
    TargetGone,

    /// <summary>The executor no longer holds the permission the action needs; the rule is stopped. Terminal.</summary>
    PermissionRevoked,

    /// <summary>The rule's configuration or the trigger fact cannot be executed as written. Terminal.</summary>
    InvalidConfiguration,

    /// <summary>The rule was disabled or auto-disabled when its execution started. Terminal.</summary>
    RuleInactive,

    /// <summary>The target or the provider deterministically rejected the action. Terminal.</summary>
    TargetRejected,

    /// <summary>The provider outcome is unknown; it is held for reconciliation and never re-fired (UC-AUT-H05). Terminal.</summary>
    OutcomeUnknown,

    /// <summary>A technical failure that may succeed on another attempt within the retry budget. Retryable.</summary>
    Transient,
}

/// <summary>The persisted code of each failure class and the retry rule. Codes are stable: never rename one.</summary>
public static class AutomationFailureClassification
{
    public static string Code(this AutomationFailureClass failureClass) => failureClass switch
    {
        AutomationFailureClass.TargetGone => "target_gone",
        AutomationFailureClass.PermissionRevoked => "permission_revoked",
        AutomationFailureClass.InvalidConfiguration => "invalid_configuration",
        AutomationFailureClass.RuleInactive => "rule_inactive",
        AutomationFailureClass.TargetRejected => "target_rejected",
        AutomationFailureClass.OutcomeUnknown => "outcome_unknown",
        AutomationFailureClass.Transient => "transient",
        _ => throw new ArgumentOutOfRangeException(nameof(failureClass), failureClass, "Unknown automation failure class."),
    };

    public static bool TryParse(string? code, out AutomationFailureClass failureClass)
    {
        foreach (var candidate in Enum.GetValues<AutomationFailureClass>())
        {
            if (string.Equals(candidate.Code(), code, StringComparison.Ordinal))
            {
                failureClass = candidate;
                return true;
            }
        }

        failureClass = default;
        return false;
    }

    /// <summary>Only a transient failure is retried; a terminal class is never retried, whatever the attempt count.</summary>
    public static bool IsRetryable(this AutomationFailureClass failureClass) =>
        failureClass == AutomationFailureClass.Transient;
}
