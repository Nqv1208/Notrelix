using Notrelix.Domain.WorkManagement.Approvals.Events;
namespace Notrelix.Domain.WorkManagement.Approvals;

public class ApprovalStep : Entity
{
    public Guid ApprovalRequestId { get; private set; }
    public Guid? ApproverUserId { get; private set; }
    public Guid? ApproverTeamId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    /// <summary>The person who decided the step: the named approver, or the team member who decided a team step.</summary>
    public Guid? DecidedByUserId { get; private set; }
    public string? Note { get; private set; }

    private ApprovalStep() : base() { }

    public static ApprovalStep Create(Guid requestId, int position, Guid? userId = null, Guid? teamId = null)
    {
        Guard.NotEmpty(requestId);
        if (position <= 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepPositionInvalid, "Step position must be greater than zero.");

        var hasUser = userId.HasValue && userId.Value != Guid.Empty;
        var hasTeam = teamId.HasValue && teamId.Value != Guid.Empty;
        if (!hasUser && !hasTeam)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepRequiresApprover, "Step must have exactly one approver (user or team).");
        if (hasUser && hasTeam)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepRequiresApprover, "Step must have exactly one approver (user or team), not both.");

        return new ApprovalStep
        {
            ApprovalRequestId = requestId,
            ApproverUserId = hasUser ? userId : null,
            ApproverTeamId = hasTeam ? teamId : null,
            Status = ApprovalStatus.Pending,
            Position = position
        };
    }

    internal void Approve(DateTimeOffset decidedAt, string? note, Guid decidedBy)
    {
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_Step_CannotApproveUnlessPending, "Only pending steps can be approved.");
        if (decidedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DecisionTimeRequired, "Decision time must be provided.");

        Status = ApprovalStatus.Approved;
        DecidedAt = decidedAt;
        DecidedByUserId = decidedBy;
        Note = note;
    }

    internal void Reject(DateTimeOffset decidedAt, string? note, Guid decidedBy)
    {
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_Step_CannotRejectUnlessPending, "Only pending steps can be rejected.");
        if (decidedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DecisionTimeRequired, "Decision time must be provided.");

        Status = ApprovalStatus.Rejected;
        DecidedAt = decidedAt;
        DecidedByUserId = decidedBy;
        Note = note;
    }
}

public class ApprovalRequest : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    /// <summary>The Rejected or Cancelled request this one asks again (re-request); null for a first request.</summary>
    public Guid? PreviousRequestId { get; private set; }

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private ApprovalRequest() : base() { }

    public void AddStep(int position, Guid addedBy, DateTimeOffset addedAt, Guid? approverUserId = null, Guid? approverTeamId = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(addedBy);
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_CannotAddStepsNonPending, "Cannot add steps to a non-pending approval request.");

        if (_steps.Any(s => s.Position == position))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DuplicateStepPosition, $"Step position {position} already exists.");

        if (approverUserId.HasValue && _steps.Any(s => s.ApproverUserId == approverUserId))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DuplicateApprover, "Duplicate approver user in approval steps.");
        if (approverTeamId.HasValue && _steps.Any(s => s.ApproverTeamId == approverTeamId))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DuplicateApprover, "Duplicate approver team in approval steps.");

        var pending = PrepareAuditUpdate(addedBy, addedAt);
        var step = ApprovalStep.Create(Id, position, approverUserId, approverTeamId);
        _steps.Add(step);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        if (step.ApproverUserId is { } approver)
        {
            RaiseDomainEvent(new ApprovalStepAssignedDomainEvent(
                AccountId, WorkspaceId, Id, step.Id, approver, RequestedByUserId, Target, Title, Description, addedAt));
        }
    }

    // A team step is decided by a current member of that team; Application supplies the teams the decider belongs to now and an absent set fails closed.
    private static void EnsureMayDecide(ApprovalStep step, Guid decidedBy, IReadOnlyCollection<Guid>? deciderTeamIds)
    {
        var allowed = step.ApproverTeamId is { } team
            ? deciderTeamIds is not null && deciderTeamIds.Contains(team)
            : step.ApproverUserId == decidedBy;
        if (!allowed)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepNotAssignedToYou, "This step is not assigned to you.");
    }

    public void Approve(Guid stepId, Guid decidedBy, DateTimeOffset decidedAt, string? note = null, IReadOnlyCollection<Guid>? deciderTeamIds = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(decidedBy);
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_CannotApproveUnlessPending, "Only pending approval requests can be approved.");
        if (decidedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DecisionTimeRequired, "Decision time must be provided.");

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepNotFound, $"Approval step '{stepId}' not found.");
        if (step.ApprovalRequestId != Id)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepNotFound, $"Approval step '{stepId}' does not belong to this request.");

        EnsureMayDecide(step, decidedBy, deciderTeamIds);

        var pending = PrepareAuditUpdate(decidedBy, decidedAt);
        step.Approve(decidedAt, note, decidedBy);

        if (_steps.All(s => s.Status == ApprovalStatus.Approved))
        {
            Status = ApprovalStatus.Approved;
            RaiseDomainEvent(new ApprovalRequestApprovedDomainEvent(AccountId, WorkspaceId, Id, decidedBy, note, decidedAt, RequestedByUserId, Target));
        }
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    public void Reject(Guid stepId, Guid decidedBy, DateTimeOffset decidedAt, string? note = null, IReadOnlyCollection<Guid>? deciderTeamIds = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(decidedBy);
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_CannotRejectUnlessPending, "Only pending approval requests can be rejected.");
        if (decidedAt == default)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_DecisionTimeRequired, "Decision time must be provided.");

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepNotFound, $"Approval step '{stepId}' not found.");
        if (step.ApprovalRequestId != Id)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_StepNotFound, $"Approval step '{stepId}' does not belong to this request.");

        EnsureMayDecide(step, decidedBy, deciderTeamIds);

        var pending = PrepareAuditUpdate(decidedBy, decidedAt);
        step.Reject(decidedAt, note, decidedBy);
        Status = ApprovalStatus.Rejected;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestRejectedDomainEvent(AccountId, WorkspaceId, Id, decidedBy, note, decidedAt, RequestedByUserId, Target));
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(cancelledBy);
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Approval_CannotCancelUnlessPending, "Only pending approval requests can be cancelled.");

        var pending = PrepareAuditUpdate(cancelledBy, cancelledAt);
        Status = ApprovalStatus.Cancelled;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestCancelledDomainEvent(AccountId, WorkspaceId, Id, cancelledBy, cancelledAt));
    }

    public static ApprovalRequest Create(Guid accountId, Guid workspaceId, ResourceRef target, string title, Guid requestedBy, DateTimeOffset createdAt, string? description = null, Guid? previousRequestId = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(title);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        Guard.NotEmpty(accountId);

        var request = new ApprovalRequest
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = ApprovalStatus.Pending,
            RequestedByUserId = requestedBy,
            PreviousRequestId = previousRequestId
        };

        request.SetAuditOnCreate(requestedBy, createdAt);
        request.RaiseDomainEvent(new ApprovalRequestCreatedDomainEvent(accountId, request.Id, workspaceId, target, createdAt));

        return request;
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
