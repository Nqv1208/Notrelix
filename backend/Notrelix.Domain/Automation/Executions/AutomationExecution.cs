using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

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
        if (Status != AutomationExecutionStatus.Queued)
            throw new BusinessRuleException("Step can only start from Queued state.");
        Status = AutomationExecutionStatus.Running;
        StartedAt = startedAt;
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException("Step can only succeed from Running state.");
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException("Step can only fail from Running state.");
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
    }
}

public class AutomationExecution : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid RuleId { get; private set; }
    public Guid TriggerId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? Error { get; private set; }
    public string? Payload { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastResponse { get; private set; }

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
            StartedAt = startedAt,
            AttemptCount = 0
        };

        execution.AddDomainEvent(new AutomationExecutionQueuedDomainEvent(workspaceId, execution.Id, ruleId, startedAt));
        return execution;
    }

    public void SetPayload(string payload)
    {
        Payload = payload;
        IncrementVersion();
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status != AutomationExecutionStatus.Queued)
            throw new BusinessRuleException("Execution can only start from Queued state.");
        Status = AutomationExecutionStatus.Running;
        StartedAt = startedAt;
        IncrementVersion();
        AddDomainEvent(new AutomationExecutionStartedDomainEvent(WorkspaceId, Id, RuleId, startedAt));
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException("Execution can only succeed from Running state.");
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
        IncrementVersion();
        AddDomainEvent(new AutomationExecutionSucceededDomainEvent(WorkspaceId, Id, RuleId, finishedAt));
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException("Execution can only fail from Running state.");
        if (string.IsNullOrWhiteSpace(error))
            throw new BusinessRuleException("Error must not be empty when execution fails.");

        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
        IncrementVersion();
        AddDomainEvent(new AutomationExecutionFailedDomainEvent(WorkspaceId, Id, RuleId, error, finishedAt));
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset cancelledAt)
    {
        if (Status != AutomationExecutionStatus.Queued && Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException("Execution can only be cancelled from Queued or Running state.");

        Status = AutomationExecutionStatus.Cancelled;
        FinishedAt = cancelledAt;
        IncrementVersion();
        AddDomainEvent(new AutomationExecutionCancelledDomainEvent(WorkspaceId, Id, RuleId, cancelledBy, cancelledAt));
    }
}
