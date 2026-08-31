using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

public record AddMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessGrantProjectionService _grantProjection;
    private readonly IActorLookupService _actorLookup;

    public AddMemberCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAccessGrantProjectionService grantProjection,
        IActorLookupService actorLookup)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _actorLookup = actorLookup;
    }

    public async Task<Result> Handle(AddMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var actor = await _actorLookup.FindAsync(request.UserId, ct);
        if (actor is null)
            throw new NotFoundException(nameof(User), request.UserId);

        var existingMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (existingMember is not null)
        {
            if (existingMember.Status == WorkspaceMemberStatus.Active)
                throw new BusinessRuleException("User is already a member of this workspace.");

            existingMember.Activate(_requestContext.UserId, _dateTimeProvider.UtcNow);
            existingMember.ChangeRole(request.Role, _requestContext.UserId, 1, _dateTimeProvider.UtcNow);
        }
        else
        {
            var member = WorkspaceMember.Create(
                workspace.AccountId,
                request.WorkspaceId,
                request.UserId,
                request.Role,
                _requestContext.UserId,
                _dateTimeProvider.UtcNow);

            _context.WorkspaceMembers.Add(member);
        }

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            workspace.AccountId,
            request.WorkspaceId,
            request.UserId,
            request.Role,
            _dateTimeProvider.UtcNow,
            ct);

        return Result.Success();
    }
}
