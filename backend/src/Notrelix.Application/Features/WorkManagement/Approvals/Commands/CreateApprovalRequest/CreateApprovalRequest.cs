using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;

public record ApprovalStepDto(Guid? ApproverUserId, Guid? ApproverTeamId);

[IdempotencyOperation("work-management.approvals.create-approval-request.v1")]
public record CreateApprovalRequestCommand(
    Guid TargetResourceId,
    ResourceType TargetResourceType,
    string Title,
    string? Description,
    List<ApprovalStepDto>? Steps)
    : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, TargetResourceId);
}

public class CreateApprovalRequestCommandHandler : IRequestHandler<CreateApprovalRequestCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateApprovalRequestCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.TargetResourceId, ct);
        if (board is null) throw new NotFoundException("Board", request.TargetResourceId);

        var target = ResourceRef.Create(request.TargetResourceType, request.TargetResourceId, board.WorkspaceId);
        var now = _dateTimeProvider.UtcNow;
        var approvalRequest = ApprovalRequest.Create(
            _requestContext.RequireAccountId(),
            board.WorkspaceId,
            target,
            request.Title,
            _requestContext.UserId,
            now);

        if (request.Description is not null)
        {
            approvalRequest.GetType().GetProperty(nameof(ApprovalRequest.Description))!
                .SetValue(approvalRequest, request.Description);
        }

        if (request.Steps is { Count: > 0 })
        {
            for (var i = 0; i < request.Steps.Count; i++)
            {
                var step = request.Steps[i];
                approvalRequest.AddStep(i + 1, _requestContext.UserId, now, step.ApproverUserId, step.ApproverTeamId);
            }
        }

        _context.ApprovalRequests.Add(approvalRequest);
        return Result<Guid>.Success(approvalRequest.Id);
    }
}
