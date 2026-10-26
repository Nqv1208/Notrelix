using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.DecideApproval;

/// <summary>
/// The named approver approves or rejects an approval request, with an optional comment. Only the person named
/// on a step may decide it (anyone else is forbidden) and only once: a step that was already decided, or a
/// request that is no longer pending, is a conflict. The approver must still be able to read the Board of the
/// Item the request is about. Any member of the Workspace reaches the use case (<c>ViewWorkspace</c>); the
/// named-approver rule is the authority.
/// </summary>
[IdempotencyOperation("work-management.approvals.decide-approval.v1")]
public record DecideApprovalCommand(Guid WorkspaceId, Guid RequestId, string Decision, string? Comment)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const int CommentMaxLength = 1000;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DecideApprovalCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IResourceVisibility visibility) : IRequestHandler<DecideApprovalCommand, Result>
{
    public async Task<Result> Handle(DecideApprovalCommand request, CancellationToken ct)
    {
        var approval = await context.ApprovalRequests
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.WorkspaceId == request.WorkspaceId && r.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(ApprovalRequest), request.RequestId);

        var userId = requestContext.UserId;
        var step = approval.Steps.FirstOrDefault(s => s.ApproverUserId == userId);
        if (step is null) throw new ForbiddenException("This approval is not assigned to you.");

        if (approval.Status != ApprovalStatus.Pending || step.Status != ApprovalStatus.Pending)
        {
            return Result.Failure(new ApplicationError(
                "work.approval.already-decided", "This approval was already decided.", ApplicationErrorType.Conflict));
        }

        if (approval.Target.Kind.Value == "work-management.board-item")
        {
            var boardId = await context.BoardItems.AsNoTracking()
                .Where(i => i.Id == approval.Target.ResourceId && i.DeletedAt == null)
                .Select(i => (Guid?)i.BoardId)
                .FirstOrDefaultAsync(ct);
            var readable = boardId is { } board
                && (await visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, ct)).Contains(board);
            if (!readable) throw new ForbiddenException("You can no longer read the item this approval is about.");
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        var now = clock.UtcNow;
        if (request.Decision == DecideApprovalCommand.Approve) approval.Approve(step.Id, userId, now, comment);
        else approval.Reject(step.Id, userId, now, comment);
        return Result.Success();
    }
}

public class DecideApprovalCommandValidator : AbstractValidator<DecideApprovalCommand>
{
    public DecideApprovalCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Decision)
            .Must(d => d is DecideApprovalCommand.Approve or DecideApprovalCommand.Reject)
            .WithMessage("The decision must be approve or reject.");
        RuleFor(x => x.Comment).MaximumLength(DecideApprovalCommand.CommentMaxLength);
    }
}
