using Notrelix.Domain.Automation.Agents.Events;

namespace Notrelix.Domain.Automation.Agents;

public enum AiAgentRunStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public class AiAgentRun : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid AiAgentId { get; private set; }
    public string TriggerType { get; private set; } = null!;
    public string? TriggerResourceType { get; private set; }
    public Guid? TriggerResourceId { get; private set; }
    public AiAgentRunStatus Status { get; private set; }
    public JsonValue Input { get; private set; } = null!;
    public JsonValue Output { get; private set; } = null!;
    public JsonValue? Error { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid? CorrelationId { get; private set; }

    private AiAgentRun() : base() { }

    public static AiAgentRun Create(
        Guid accountId,
        Guid workspaceId,
        Guid aiAgentId,
        string triggerType,
        string? triggerResourceType,
        Guid? triggerResourceId,
        JsonValue input,
        Guid? actorUserId,
        Guid? correlationId,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(aiAgentId);
        Guard.NotNullOrWhiteSpace(triggerType);
        Guard.NotNull(input);

        var run = new AiAgentRun
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            AiAgentId = aiAgentId,
            TriggerType = triggerType,
            TriggerResourceType = triggerResourceType,
            TriggerResourceId = triggerResourceId,
            Status = AiAgentRunStatus.Queued,
            Input = input,
            Output = JsonValue.EmptyObject(),
            ActorUserId = actorUserId,
            CorrelationId = correlationId
        };

        run.SetAuditOnCreate(actorUserId, createdAt);
        run.RaiseDomainEvent(new AiAgentRunQueuedDomainEvent(accountId, workspaceId, run.Id, aiAgentId, createdAt));
        return run;
    }

    public void Start(DateTimeOffset startedAt)
    {
        EnsureNotDeleted();
        if (Status != AiAgentRunStatus.Queued)
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Agent_InvalidStatusTransition, "Run can only start from Queued state.");

        Status = AiAgentRunStatus.Running;
        StartedAt = startedAt;
        SetAuditOnUpdate(ActorUserId, startedAt);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentRunStartedDomainEvent(AccountId, WorkspaceId, Id, AiAgentId, startedAt));
    }

    public void Succeed(JsonValue output, DateTimeOffset finishedAt)
    {
        EnsureNotDeleted();
        if (Status != AiAgentRunStatus.Running)
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Agent_InvalidStatusTransition, "Run can only succeed from Running state.");
        Guard.NotNull(output);

        Status = AiAgentRunStatus.Succeeded;
        Output = output;
        FinishedAt = finishedAt;
        SetAuditOnUpdate(ActorUserId, finishedAt);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentRunSucceededDomainEvent(AccountId, WorkspaceId, Id, AiAgentId, finishedAt));
    }

    public void Fail(JsonValue error, DateTimeOffset finishedAt)
    {
        EnsureNotDeleted();
        if (Status != AiAgentRunStatus.Running)
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Agent_InvalidStatusTransition, "Run can only fail from Running state.");
        Guard.NotNull(error);

        Status = AiAgentRunStatus.Failed;
        Error = error;
        FinishedAt = finishedAt;
        SetAuditOnUpdate(ActorUserId, finishedAt);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentRunFailedDomainEvent(AccountId, WorkspaceId, Id, AiAgentId, error.ToString(), finishedAt));
    }

    public void Cancel(Guid? cancelledBy, DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status != AiAgentRunStatus.Queued && Status != AiAgentRunStatus.Running)
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Agent_InvalidStatusTransition, "Run can only be cancelled from Queued or Running state.");

        Status = AiAgentRunStatus.Cancelled;
        FinishedAt = cancelledAt;
        SetAuditOnUpdate(cancelledBy, cancelledAt);
        IncrementVersion();
        RaiseDomainEvent(new AiAgentRunCancelledDomainEvent(AccountId, WorkspaceId, Id, AiAgentId, cancelledBy, cancelledAt));
    }
}
