using Notrelix.Application.Common.Realtime;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Rules;

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
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IWorkspaceOwnerUpdateLocker _locker;

    private readonly IAccessChangeSignal _access;

    public RemoveMemberCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceGrantProjectionService grantProjection,
        IWorkspaceOwnerUpdateLocker locker,
        IAccessChangeSignal access)
    {
        _access = access;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _locker = locker;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        var workspace = await _locker.LockWorkspaceAsync(request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var actorRole = await WorkspaceActorRoleLookup.GetActiveActorRoleAsync(
            _context, workspace.Id, _requestContext.UserId, ct);
        WorkspaceMemberRules.EnsureActorCanActOnMember(actorRole, member.Role);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);

        member.Remove(activeOwnerCount, _requestContext.UserId, _dateTimeProvider.UtcNow);

        await _grantProjection.RevokeWorkspaceMemberGrantAsync(
            workspace.AccountId,
            workspace.Id,
            request.UserId,
            _dateTimeProvider.UtcNow,
            ct);

        _access.WorkspacePerson(request.WorkspaceId, request.UserId);
        return Result.Success();
    }
}
