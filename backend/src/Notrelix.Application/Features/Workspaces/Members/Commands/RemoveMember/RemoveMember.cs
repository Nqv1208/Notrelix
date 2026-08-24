using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

public record RemoveMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.RemoveMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public RemoveMemberCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);

        member.Remove(activeOwnerCount, _requestContext.UserId, _dateTimeProvider.UtcNow);

        await _grantProjection.RevokeWorkspaceMemberGrantAsync(
            workspace.AccountId,
            workspace.Id,
            request.UserId,
            _dateTimeProvider.UtcNow,
            ct);

        return Result.Success();
    }
}
