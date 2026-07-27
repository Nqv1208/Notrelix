using Notrelix.Domain.Automation.Executions.Events;
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
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Step_CannotStartUnlessQueued, "Step can only start from Queued state.");
        Status = AutomationExecutionStatus.Running;
        StartedAt = startedAt;
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Step_CannotSucceedUnlessRunning, "Step can only succeed from Running state.");
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Step_CannotFailUnlessRunning, "Step can only fail from Running state.");
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
    }
}

public class AutomationExecution : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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

    public static AutomationExecution Create(Guid accountId, Guid workspaceId, Guid ruleId, Guid triggerId, DateTimeOffset startedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(ruleId);
        Guard.NotEmpty(triggerId);

        var execution = new AutomationExecution
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RuleId = ruleId,
            TriggerId = triggerId,
            Status = AutomationExecutionStatus.Queued,
            StartedAt = startedAt,
            AttemptCount = 0
        };

        execution.SetAuditOnCreate(null, startedAt);
        execution.RaiseDomainEvent(new AutomationExecutionQueuedDomainEvent(accountId, workspaceId, execution.Id, ruleId, startedAt));
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
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Execution_CannotStartUnlessQueued, "Execution can only start from Queued state.");
        Status = AutomationExecutionStatus.Running;
        StartedAt = startedAt;
        IncrementVersion();
        RaiseDomainEvent(new AutomationExecutionStartedDomainEvent(AccountId, WorkspaceId, Id, RuleId, startedAt));
    }

    public void Succeed(DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Execution_CannotSucceedUnlessRunning, "Execution can only succeed from Running state.");
        Status = AutomationExecutionStatus.Succeeded;
        FinishedAt = finishedAt;
        IncrementVersion();
        RaiseDomainEvent(new AutomationExecutionSucceededDomainEvent(AccountId, WorkspaceId, Id, RuleId, finishedAt));
    }

    public void Fail(string error, DateTimeOffset finishedAt)
    {
        if (Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Execution_CannotFailUnlessRunning, "Execution can only fail from Running state.");
        if (string.IsNullOrWhiteSpace(error))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Execution_ErrorRequiredOnFail, "Error must not be empty when execution fails.");

        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
        IncrementVersion();
        RaiseDomainEvent(new AutomationExecutionFailedDomainEvent(AccountId, WorkspaceId, Id, RuleId, error, finishedAt));
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset cancelledAt)
    {
        if (Status != AutomationExecutionStatus.Queued && Status != AutomationExecutionStatus.Running)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Execution_CannotCancelUnlessQueuedOrRunning, "Execution can only be cancelled from Queued or Running state.");

        Status = AutomationExecutionStatus.Cancelled;
        FinishedAt = cancelledAt;
        IncrementVersion();
        RaiseDomainEvent(new AutomationExecutionCancelledDomainEvent(AccountId, WorkspaceId, Id, RuleId, cancelledBy, cancelledAt));
    }
}
