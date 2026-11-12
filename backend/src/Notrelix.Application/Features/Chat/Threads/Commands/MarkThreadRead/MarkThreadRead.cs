using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Threads.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.ReadStates;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Threads.Commands.MarkThreadRead;

/// <summary>
/// Moves the caller's read pointer in a followed thread forward to the reply they have seen (UC-CHT-F08). A lower value
/// changes nothing, and a value above the reply count reads the thread to its end. The caller must be an active member
/// of the thread's space; otherwise it is the same NotFound as a missing thread. A thread the caller does not follow
/// has no pointer, so nothing moves and the unread count stays 0.
/// </summary>
public record MarkThreadReadCommand(Guid WorkspaceId, Guid ThreadId, long UpToReplySequence)
    : ICommand<Result<ThreadReadDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class MarkThreadReadCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IChatThreadReadStore readStore) : IRequestHandler<MarkThreadReadCommand, Result<ThreadReadDto>>
{
    public async Task<Result<ThreadReadDto>> Handle(MarkThreadReadCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var thread = await db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(
            t => t.RootMessageId == request.ThreadId && t.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(MessageThread), request.ThreadId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, thread.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), thread.SpaceId);

        var target = ReadBoundary.Clamp(request.UpToReplySequence, thread.ReplyCount);
        await readStore.AdvanceAsync(request.WorkspaceId, request.ThreadId, userId, target, cancellationToken);

        var follower = await db.ChatThreadFollowers.AsNoTracking().FirstOrDefaultAsync(
            f => f.RootMessageId == request.ThreadId && f.UserId == userId, cancellationToken);
        return Result<ThreadReadDto>.Success(new ThreadReadDto(request.ThreadId, follower?.UnreadReplies(thread.ReplyCount) ?? 0));
    }
}

public class MarkThreadReadCommandValidator : AbstractValidator<MarkThreadReadCommand>
{
    public MarkThreadReadCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.UpToReplySequence).GreaterThanOrEqualTo(0);
    }
}
