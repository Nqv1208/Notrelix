using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;

/// <summary>
/// The caller joins a public channel (UC-CHT-A05). A private channel is the same NotFound as a missing one, and a
/// Guest is refused (403). Joining again changes nothing.
/// </summary>
public record JoinChannelCommand(Guid WorkspaceId, Guid ChannelId)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class JoinChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<JoinChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(JoinChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        if (string.Equals(caller.Role, ChannelAccess.GuestRole, StringComparison.Ordinal))
            throw new ForbiddenException("A Guest joins a channel only when they are added to it.");

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        if (space.IsPrivate)
            throw new NotFoundException(nameof(Space), space.Id);

        space.AddMember(userId, MembershipRole.Member, clock.UtcNow);
        return Result<ChannelDto>.Success(ChannelDto.From(space, space.GetActiveMembership(userId)));
    }
}
