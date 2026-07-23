namespace Notrelix.Domain.WorkManagement.Approvals;

public class ApprovalStep : Entity
{
    public Guid ApprovalRequestId { get; private set; }
    public Guid? ApproverUserId { get; private set; }
    public Guid? ApproverTeamId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? Note { get; private set; }

    private ApprovalStep() : base() { }

    public static ApprovalStep Create(Guid requestId, int position, Guid? userId = null, Guid? teamId = null)
    {
        return new ApprovalStep
        {
            ApprovalRequestId = requestId,
            ApproverUserId = userId,
            ApproverTeamId = teamId,
            Status = ApprovalStatus.Pending,
            Position = position
        };
    }

    public void Approve(DateTimeOffset decidedAt, string? note = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_Step_CannotApproveUnlessPending, "Only pending steps can be approved.");

        Status = ApprovalStatus.Approved;
        DecidedAt = decidedAt;
        Note = note;
    }

    public void Reject(DateTimeOffset decidedAt, string? note = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_Step_CannotRejectUnlessPending, "Only pending steps can be rejected.");

        Status = ApprovalStatus.Rejected;
        DecidedAt = decidedAt;
        Note = note;
    }
}

public class ApprovalRequest : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public Guid RequestedByUserId { get; private set; }

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private ApprovalRequest() : base() { }

    public void AddStep(int position, Guid? approverUserId = null, Guid? approverTeamId = null)
    {
        EnsureNotDeleted();
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_CannotAddStepsNonPending, "Cannot add steps to a non-pending approval request.");

        var step = ApprovalStep.Create(Id, position, approverUserId, approverTeamId);
        _steps.Add(step);
    }

    public void Approve(Guid stepId, Guid decidedBy, DateTimeOffset decidedAt, string? note = null)
    {
        EnsureNotDeleted();
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_CannotApproveUnlessPending, "Only pending approval requests can be approved.");

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new NotFoundException(nameof(ApprovalStep), stepId);

        step.Approve(decidedAt, note);

        if (_steps.All(s => s.Status == ApprovalStatus.Approved))
        {
            Status = ApprovalStatus.Approved;
            RaiseDomainEvent(new ApprovalRequestApprovedDomainEvent(AccountId, WorkspaceId, Id, decidedBy, note, decidedAt));
        }

        SetAuditOnUpdate(decidedBy, decidedAt);
        IncrementVersion();
    }

    public void Reject(Guid stepId, Guid decidedBy, DateTimeOffset decidedAt, string? note = null)
    {
        EnsureNotDeleted();
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_CannotRejectUnlessPending, "Only pending approval requests can be rejected.");

        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new NotFoundException(nameof(ApprovalStep), stepId);

        step.Reject(decidedAt, note);
        Status = ApprovalStatus.Rejected;

        SetAuditOnUpdate(decidedBy, decidedAt);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestRejectedDomainEvent(AccountId, WorkspaceId, Id, decidedBy, note, decidedAt));
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status != ApprovalStatus.Pending)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Approval_CannotCancelUnlessPending, "Only pending approval requests can be cancelled.");

        Status = ApprovalStatus.Cancelled;
        SetAuditOnUpdate(cancelledBy, cancelledAt);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestCancelledDomainEvent(AccountId, WorkspaceId, Id, cancelledBy, cancelledAt));
    }

    public static ApprovalRequest Create(Guid accountId, Guid workspaceId, ResourceRef target, string title, Guid requestedBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(title);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        Guard.NotEmpty(accountId);

        var request = new ApprovalRequest
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            Title = title.Trim(),
            Status = ApprovalStatus.Pending,
            RequestedByUserId = requestedBy
        };

        request.SetAuditOnCreate(requestedBy, createdAt);
        request.RaiseDomainEvent(new ApprovalRequestCreatedDomainEvent(accountId, workspaceId, request.Id, target, createdAt));

        return request;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new ApprovalRequestRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
