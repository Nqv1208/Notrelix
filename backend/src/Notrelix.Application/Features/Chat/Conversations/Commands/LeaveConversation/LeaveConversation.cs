using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Conversations.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Conversations.Commands.LeaveConversation;

/// <summary>
/// The caller leaves a group conversation (UC-CHT-E04). The remaining participants see a system message, and the caller gets no
/// further frame of the group (the access signal closes their subscription). When the last participant leaves, the conversation
/// closes (a soft delete; its purge follows the retention rule). A direct conversation cannot be left (400 kind-not-supported).
/// Leaving a group the caller is not in is NotFound, the same as a missing group.
/// </summary>
public record LeaveConversationCommand(Guid WorkspaceId, Guid ConversationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class LeaveConversationCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages,
    IAccessChangeSignal access) : IRequestHandler<LeaveConversationCommand, Result>
{
    public async Task<Result> Handle(LeaveConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ConversationAccess.LoadParticipantAsync(db, request.WorkspaceId, request.ConversationId, userId, cancellationToken);

        var now = clock.UtcNow;
        if (!space.LeaveConversation(userId, now))
            return Result.Success();

        access.ChannelPerson(request.WorkspaceId, space.Id, userId);
        if (space.ActiveMemberCount() == 0)
        {
            space.CloseIfNoParticipants(userId, now);
            return Result.Success();
        }

        await systemMessages.AnnounceAsync(space, userId, ChatSystemMessages.MemberLeft, cancellationToken);
        return Result.Success();
    }
}
