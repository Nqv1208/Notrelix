using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Members.Commands.SetChannelMemberRole;

/// <summary>
/// A manager, Workspace Owner or Admin makes a member of a channel a manager, or a manager a plain member (UC-CHT-A07). The last manager
/// of a live channel cannot be made a member (422 last-manager), so the one who wants to leave names a successor first. A person who is
/// not in the channel is the same NotFound as a missing one; the same role again changes nothing.
/// </summary>
public record SetChannelMemberRoleCommand(Guid WorkspaceId, Guid ChannelId, Guid UserId, string Role)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetChannelMemberRoleCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatAuditFacts audit) : IRequestHandler<SetChannelMemberRoleCommand, Result>
{
    public async Task<Result> Handle(SetChannelMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        if (!space.IsActiveMember(request.UserId))
            throw new NotFoundException(nameof(Domain.Chat.Spaces.Space), request.ChannelId);

        var role = SpaceWire.MembershipRoleFromWire(request.Role);
        // Only an active, non-Guest Workspace member can manage a channel (CHT-004): the stricter answer where nothing says otherwise.
        if (role == MembershipRole.Manager)
        {
            var target = await members.FindAsync(request.WorkspaceId, request.UserId, cancellationToken);
            if (target is not { IsActive: true } || ChannelAccess.IsGuest(target))
                throw new ForbiddenException("Only an active, non-Guest Workspace member can be a channel manager.");
        }

        if (space.SetMemberRole(request.UserId, role))
            await audit.MemberRoleChangedAsync(space, request.UserId, request.Role, cancellationToken);
        return Result.Success();
    }
}

public class SetChannelMemberRoleCommandValidator : AbstractValidator<SetChannelMemberRoleCommand>
{
    public SetChannelMemberRoleCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).Must(role => role is "manager" or "member").WithMessage("role must be manager or member.");
    }
}
