namespace Notrelix.Domain.Automation.Executions;

public enum AutomationExecutionStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled,

    /// <summary>UC-AUT-C02: the rule's conditions were not met, so no action ran. A skip is not a failure.</summary>
    Skipped
}
