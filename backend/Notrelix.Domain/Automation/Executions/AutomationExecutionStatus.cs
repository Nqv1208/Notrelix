namespace Notrelix.Domain.Automation.Executions;

public enum AutomationExecutionStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}
