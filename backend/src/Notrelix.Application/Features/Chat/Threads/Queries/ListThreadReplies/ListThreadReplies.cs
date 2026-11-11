using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Threads.DTOs;
using Notrelix.Application.Features.Chat.Threads.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Threads.Queries.ListThreadReplies;

/// <summary>
/// A page of a thread's replies in thread sequence order, with the root and the thread summary (UC-CHT-F03). The cursor is
/// After: the thread sequence of the last reply the caller has, and the page holds the replies above it. Deleted replies
/// stay as tombstones. The caller must see the space; otherwise it is the same NotFound as a missing root.
/// </summary>
public record ListThreadRepliesQuery(Guid WorkspaceId, Guid RootMessageId, long? After, int Limit)
    : IQuery<Result<ThreadRepliesPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The page size when the caller names none (UC-CHT-F03).</summary>
    public const int DefaultLimit = ChatLimits.PageSizeDefault;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListThreadRepliesQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<ListThreadRepliesQuery, Result<ThreadRepliesPageDto>>
{
    public async Task<Result<ThreadRepliesPageDto>> Handle(ListThreadRepliesQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var root = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.RootMessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.RootMessageId);
        if (root.IsThreadReply)
            throw new NotFoundException(nameof(Message), request.RootMessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, root.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var thread = await db.ChatThreads.AsNoTracking().FirstOrDefaultAsync(t => t.RootMessageId == root.Id, cancellationToken);
        var follower = await db.ChatThreadFollowers.AsNoTracking().FirstOrDefaultAsync(
            f => f.RootMessageId == root.Id && f.UserId == userId, cancellationToken);

        var after = request.After ?? 0;
        var rows = await db.ChatMessages.AsNoTracking()
            .Where(m => m.ThreadRootId == root.Id && m.ThreadSequence > after)
            .OrderBy(m => m.ThreadSequence)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        var items = await ReplyPreviews.DtosAsync(db, request.WorkspaceId, rows.Take(request.Limit).ToList(), cancellationToken);
        return Result<ThreadRepliesPageDto>.Success(new ThreadRepliesPageDto(
            await ReplyPreviews.DtoAsync(db, request.WorkspaceId, root, cancellationToken),
            ThreadSummaries.For(root.Id, root.SpaceId, thread, follower, now),
            items,
            HasNewer: rows.Count > request.Limit));
    }
}

public class ListThreadRepliesQueryValidator : AbstractValidator<ListThreadRepliesQuery>
{
    public ListThreadRepliesQueryValidator()
    {
        RuleFor(x => x.RootMessageId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.PageSizeMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.PageSizeMax}.");
        RuleFor(x => x.After).GreaterThanOrEqualTo(0).When(x => x.After is not null);
    }
}
