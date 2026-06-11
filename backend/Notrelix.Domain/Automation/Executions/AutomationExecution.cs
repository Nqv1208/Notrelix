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

    public void Start()
    {
        Status = AutomationExecutionStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Succeed()
    {
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string error)
    {
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}

public class AutomationExecution : AggregateRoot
{
    public Guid RuleId { get; private set; }
    public Guid TriggerId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? Error { get; private set; }

    private readonly List<AutomationExecutionStep> _steps = new();
    public IReadOnlyCollection<AutomationExecutionStep> Steps => _steps.AsReadOnly();

    private AutomationExecution() : base() { }

    public static AutomationExecution Create(Guid ruleId, Guid triggerId)
    {
        Guard.NotEmpty(ruleId);
        Guard.NotEmpty(triggerId);

        var execution = new AutomationExecution
        {
            RuleId = ruleId,
            TriggerId = triggerId,
            Status = AutomationExecutionStatus.Queued,
            StartedAt = DateTimeOffset.UtcNow
        };

        execution.AddDomainEvent(new AutomationExecutionStartedEvent(execution.Id, ruleId));
        return execution;
    }

    public void Succeed()
    {
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new AutomationExecutionSucceededEvent(Id, RuleId));
    }

    public void Fail(string error)
    {
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new AutomationExecutionFailedEvent(Id, RuleId, error));
    }
}
