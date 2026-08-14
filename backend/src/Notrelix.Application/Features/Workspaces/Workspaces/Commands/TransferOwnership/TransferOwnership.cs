using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

public record TransferOwnershipCommand(
    Guid WorkspaceId,
    Guid NewOwnerId,
    long ExpectedVersion
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public TransferOwnershipCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        var currentOwnerId = _requestContext.UserId;

        if (currentOwnerId == request.NewOwnerId)
            throw new BusinessRuleException("Cannot transfer ownership to yourself.");

        var currentOwner = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(
                m => m.WorkspaceId == request.WorkspaceId
                    && m.UserId == currentOwnerId
                    && m.Role == WorkspaceRole.Owner
                    && m.Status == WorkspaceMemberStatus.Active,
                ct);

        if (currentOwner is null)
            throw new NotFoundException(nameof(WorkspaceMember), currentOwnerId);

        var newOwner = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(
                m => m.WorkspaceId == request.WorkspaceId
                    && m.UserId == request.NewOwnerId
                    && m.Status == WorkspaceMemberStatus.Active,
                ct);

        if (newOwner is null)
            throw new NotFoundException(nameof(WorkspaceMember), request.NewOwnerId);

        var now = _dateTimeProvider.UtcNow;

        newOwner.PromoteToOwner(currentOwnerId, now);
        currentOwner.ChangeRole(WorkspaceRole.Admin, currentOwnerId, 2, now);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            newOwner.AccountId,
            request.WorkspaceId,
            request.NewOwnerId,
            WorkspaceRole.Owner,
            now,
            ct);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            currentOwner.AccountId,
            request.WorkspaceId,
            currentOwnerId,
            WorkspaceRole.Admin,
            now,
            ct);

        return Result.Success();
    }
}
