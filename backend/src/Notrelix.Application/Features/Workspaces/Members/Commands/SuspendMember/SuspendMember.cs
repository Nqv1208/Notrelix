using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;

public record SuspendMemberCommand(
    Guid WorkspaceId,
    Guid UserId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.RemoveMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SuspendMemberCommandHandler : IRequestHandler<SuspendMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IWorkspaceOwnerUpdateLocker _locker;

    private readonly IAccessChangeSignal _access;

    public SuspendMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IWorkspaceGrantProjectionService grantProjection, IWorkspaceOwnerUpdateLocker locker, IAccessChangeSignal access)
    {
        _access = access;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _locker = locker;
    }

    public async Task<Result> Handle(SuspendMemberCommand request, CancellationToken ct)
    {
        var workspace = await _locker.LockWorkspaceAsync(request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var actorRole = await WorkspaceActorRoleLookup.GetActiveActorRoleAsync(
            _context, request.WorkspaceId, _requestContext.UserId, ct);
        WorkspaceMemberRules.EnsureActorCanActOnMember(actorRole, member.Role);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == request.WorkspaceId && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);

        member.Suspend(_requestContext.UserId, _dateTimeProvider.UtcNow, activeOwnerCount);

        await _grantProjection.RevokeWorkspaceMemberGrantAsync(
            workspace.AccountId,
            request.WorkspaceId,
            request.UserId,
            _dateTimeProvider.UtcNow,
            ct);

        _access.WorkspacePerson(request.WorkspaceId, request.UserId);
        return Result.Success();
    }
}
