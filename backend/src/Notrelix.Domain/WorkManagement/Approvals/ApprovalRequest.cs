using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

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
}

public class ApprovalRequest : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public Guid RequestedByUserId { get; private set; }

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private ApprovalRequest() : base() { }

    public static ApprovalRequest Create(Guid workspaceId, ResourceRef target, string title, Guid requestedBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(title);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        var request = new ApprovalRequest
        {
            WorkspaceId = workspaceId,
            Target = target,
            Title = title.Trim(),
            Status = ApprovalStatus.Pending,
            RequestedByUserId = requestedBy
        };

        request.SetAuditOnCreate(requestedBy, createdAt);
        request.AddDomainEvent(new ApprovalRequestCreatedDomainEvent(request.Id, workspaceId, target, createdAt));

        return request;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new ApprovalRequestSoftDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new ApprovalRequestRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
