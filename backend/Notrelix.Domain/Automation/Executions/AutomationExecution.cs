using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Executions;

public class AutomationExecutionStep : Entity
{
    public Guid ExecutionId { get; private set; }
    public Guid ActionId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? Error { get; private set; }

    private AutomationExecutionStep() : base() { }

    public static AutomationExecutionStep Create(Guid executionId, Guid actionId)
    {
        return new AutomationExecutionStep
        {
            ExecutionId = executionId,
            ActionId = actionId,
            Status = AutomationExecutionStatus.Queued
        };
    }

    public void Start(DateTimeOffset startedAt)
    {
        Status = AutomationExecutionStatus.Running;
        StartedAt = startedAt;
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
    }
}

public class AutomationExecution : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid RuleId { get; private set; }
    public Guid TriggerId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? Error { get; private set; }

    private readonly List<AutomationExecutionStep> _steps = new();
    public IReadOnlyCollection<AutomationExecutionStep> Steps => _steps.AsReadOnly();

    private AutomationExecution() : base() { }

    public static AutomationExecution Create(Guid workspaceId, Guid ruleId, Guid triggerId, DateTimeOffset startedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(ruleId);
        Guard.NotEmpty(triggerId);

        var execution = new AutomationExecution
        {
            WorkspaceId = workspaceId,
            RuleId = ruleId,
            TriggerId = triggerId,
            Status = AutomationExecutionStatus.Queued,
            StartedAt = startedAt
        };

        execution.AddDomainEvent(new AutomationExecutionStartedEvent(workspaceId, execution.Id, ruleId, startedAt));
        return execution;
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
        AddDomainEvent(new AutomationExecutionSucceededEvent(WorkspaceId, Id, RuleId, finishedAt));
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
        AddDomainEvent(new AutomationExecutionFailedEvent(WorkspaceId, Id, RuleId, error, finishedAt));
    }
}
