using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;

/// <summary>
/// A person leaves a channel (their own id) or a manager, Workspace Owner or Admin removes someone else. The last
/// manager of a live channel cannot go (422 last-manager). Removing someone who is not a member changes nothing.
/// </summary>
public record RemoveChannelMemberCommand(Guid WorkspaceId, Guid ChannelId, Guid UserId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RemoveChannelMemberCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages,
    IAccessChangeSignal access) : IRequestHandler<RemoveChannelMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveChannelMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        if (request.UserId != userId)
            ChannelAccess.RequireManager(space, caller, userId);

        if (space.RemoveMember(request.UserId, clock.UtcNow))
        {
            var content = request.UserId == userId ? ChatSystemMessages.MemberLeft : ChatSystemMessages.MemberRemoved;
            await systemMessages.AnnounceAsync(space, userId, content, cancellationToken);
            // The person's live subscription to this channel closes at once (UC-CHT-C06).
            access.ChannelPerson(request.WorkspaceId, request.ChannelId, request.UserId);
        }
        return Result.Success();
    }
}

public class RemoveChannelMemberCommandValidator : AbstractValidator<RemoveChannelMemberCommand>
{
    public RemoveChannelMemberCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
