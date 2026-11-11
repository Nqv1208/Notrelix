using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Threads.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Threads.Commands.FollowThread;

/// <summary>
/// Sets the caller's follow state on a thread (UC-CHT-F04). Following with muted true follows without notifications;
/// following false removes the follow. The thread must exist and the caller must be an active member of its space; a
/// thread the caller cannot see, or a missing one, is the same NotFound. A new follow starts with the replies already
/// posted read, so only later replies count as unread.
/// </summary>
public record FollowThreadCommand(Guid WorkspaceId, Guid ThreadId, bool Following, bool Muted)
    : ICommand<Result<ThreadFollowDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class FollowThreadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<FollowThreadCommand, Result<ThreadFollowDto>>
{
    public async Task<Result<ThreadFollowDto>> Handle(FollowThreadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var thread = await db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(
            t => t.RootMessageId == request.ThreadId && t.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(MessageThread), request.ThreadId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, thread.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), thread.SpaceId);

        var follower = await db.ChatThreadFollowers.FirstOrDefaultAsync(
            f => f.RootMessageId == request.ThreadId && f.UserId == userId, cancellationToken);

        if (!request.Following)
        {
            if (follower is not null)
                db.ChatThreadFollowers.Remove(follower);
            return Result<ThreadFollowDto>.Success(new ThreadFollowDto(request.ThreadId, Following: false, Muted: false));
        }

        if (follower is null)
        {
            db.ChatThreadFollowers.Add(ThreadFollower.Follow(
                accountId, request.WorkspaceId, request.ThreadId, userId, request.Muted, readReplySequence: thread.ReplyCount));
        }
        else
        {
            follower.SetMuted(request.Muted);
        }

        return Result<ThreadFollowDto>.Success(new ThreadFollowDto(request.ThreadId, Following: true, Muted: request.Muted));
    }
}

public class FollowThreadCommandValidator : AbstractValidator<FollowThreadCommand>
{
    public FollowThreadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
    }
}
