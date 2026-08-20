using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;

public record ActivateMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.RemoveMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ActivateMemberCommandHandler : IRequestHandler<ActivateMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;

    public ActivateMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IAccessGrantProjectionService grantProjection)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result> Handle(ActivateMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        member.Activate(_requestContext.UserId, _dateTimeProvider.UtcNow);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            workspace.Id,
            request.UserId,
            member.Role,
            _dateTimeProvider.UtcNow,
            ct);

        return Result.Success();
    }
}
