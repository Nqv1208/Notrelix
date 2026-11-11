using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
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
    IDateTimeProvider clock) : IRequestHandler<RemoveChannelMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveChannelMemberCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        if (request.UserId != userId)
            ChannelAccess.RequireManager(space, caller, userId);

        space.RemoveMember(request.UserId, clock.UtcNow);
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
