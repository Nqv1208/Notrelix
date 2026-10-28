using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.RequestItemApproval;

public sealed record ApprovalRequestIdDto(Guid Id);

/// <summary>
/// The caller asks one person to approve an Item. The approver must be someone other than the caller who can
/// read the Item's Board; the request has one step naming them, the Item's name as its title and the optional
/// note as its description. Asking the same person about the same Item while their request is still pending
/// is refused. Editing the Item (<c>UpdateItem</c>) is enough to ask.
/// </summary>
[IdempotencyOperation("work-management.approvals.request-item-approval.v1")]
public record RequestItemApprovalCommand(Guid ItemId, Guid ApproverUserId, string? Note)
    : ICommand<Result<ApprovalRequestIdDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public const int NoteMaxLength = 1000;

    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class RequestItemApprovalCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IResourceVisibility visibility) : IRequestHandler<RequestItemApprovalCommand, Result<ApprovalRequestIdDto>>
{
    public async Task<Result<ApprovalRequestIdDto>> Handle(RequestItemApprovalCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.ItemId);

        var userId = requestContext.UserId;
        if (request.ApproverUserId == userId)
        {
            return Result<ApprovalRequestIdDto>.Failure(new ApplicationError(
                "work.approval.approver-is-requester", "Ask someone else to approve.", ApplicationErrorType.BusinessRule));
        }

        var readableBoards = await visibility.VisibleBoardIdsAsync(item.WorkspaceId, request.ApproverUserId, ct);
        if (!readableBoards.Contains(item.BoardId))
        {
            return Result<ApprovalRequestIdDto>.Failure(new ApplicationError(
                "work.approval.approver-cannot-read-item", "The approver cannot read this item.", ApplicationErrorType.BusinessRule));
        }

        var kind = ResourceKind.Create("work-management.board-item");
        var pending = await context.ApprovalRequests.AsNoTracking()
            .AnyAsync(r => r.Target.Kind == kind
                           && r.Target.ResourceId == item.Id
                           && r.Status == ApprovalStatus.Pending
                           && r.DeletedAt == null
                           && r.Steps.Any(s => s.ApproverUserId == request.ApproverUserId && s.Status == ApprovalStatus.Pending), ct);
        if (pending)
        {
            return Result<ApprovalRequestIdDto>.Failure(new ApplicationError(
                "work.approval.already-requested", "This person was already asked to approve this item.", ApplicationErrorType.Conflict));
        }

        var now = clock.UtcNow;
        var title = item.Name.Length > 200 ? item.Name[..200] : item.Name;
        var approval = ApprovalRequest.Create(
            requestContext.RequireAccountId(),
            item.WorkspaceId,
            ResourceRef.Create(kind, item.Id, item.WorkspaceId),
            title,
            userId,
            now,
            request.Note);
        approval.AddStep(1, userId, now, request.ApproverUserId);

        context.ApprovalRequests.Add(approval);
        return Result<ApprovalRequestIdDto>.Success(new ApprovalRequestIdDto(approval.Id));
    }
}

public class RequestItemApprovalCommandValidator : AbstractValidator<RequestItemApprovalCommand>
{
    public RequestItemApprovalCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(RequestItemApprovalCommand.NoteMaxLength);
    }
}
