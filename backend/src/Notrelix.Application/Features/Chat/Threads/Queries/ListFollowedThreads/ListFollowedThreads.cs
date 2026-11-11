using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Threads.DTOs;
using Notrelix.Application.Features.Chat.Threads.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;

/// <summary>
/// The threads the caller follows, most recent reply first, with their unread reply counts (UC-CHT-F08). Only threads
/// whose space the caller is still an active member of and that is not deleted are listed, so a thread leaves the list
/// when access ends. The cursor is the last reply time of the previous page (exclusive); the page size is 1 to 100.
/// </summary>
public record ListFollowedThreadsQuery(Guid WorkspaceId, string? Cursor, int Limit)
    : IQuery<Result<FollowedThreadPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The page size when the caller names none.</summary>
    public const int DefaultLimit = ChatLimits.PageSizeDefault;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListFollowedThreadsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<ListFollowedThreadsQuery, Result<FollowedThreadPageDto>>
{
    public async Task<Result<FollowedThreadPageDto>> Handle(ListFollowedThreadsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        DateTimeOffset? before = null;
        if (request.Cursor is not null)
        {
            if (!long.TryParse(request.Cursor, out var ticks))
                throw new Notrelix.Application.Common.Exceptions.ValidationException("cursor is not valid.");
            before = new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        var memberships = db.ChatMemberships.AsNoTracking().Where(m => m.UserId == userId && m.LeftAt == null);
        var liveSpaces = db.ChatSpaces.AsNoTracking().Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null);

        var query = db.ChatThreadFollowers.AsNoTracking()
            .Where(f => f.WorkspaceId == request.WorkspaceId && f.UserId == userId)
            .Join(db.ChatThreads.AsNoTracking(), f => f.RootMessageId, t => t.RootMessageId, (f, t) => new { f, t })
            .Where(x => memberships.Any(m => m.SpaceId == x.t.SpaceId) && liveSpaces.Any(s => s.Id == x.t.SpaceId));
        if (before is { } cursor)
            query = query.Where(x => x.t.LastReplyAt < cursor);

        var rows = await query
            .OrderByDescending(x => x.t.LastReplyAt)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var page = rows.Take(request.Limit).ToList();
        var rootIds = page.Select(x => x.t.RootMessageId).ToList();
        var roots = await db.ChatMessages.AsNoTracking()
            .Where(m => rootIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        var rootDtos = await ReplyPreviews.DtosAsync(db, request.WorkspaceId, rootIds.Select(id => roots[id]).ToList(), cancellationToken);
        var rootById = rootDtos.ToDictionary(r => r.Id);

        var now = clock.UtcNow;
        var items = page.Select(x => new FollowedThreadDto(
            rootById[x.t.RootMessageId],
            ThreadSummaries.For(x.t.RootMessageId, x.t.SpaceId, x.t, x.f, now))).ToList();

        var next = rows.Count > request.Limit && page.Count > 0
            ? page[^1].t.LastReplyAt.UtcTicks.ToString()
            : null;
        return Result<FollowedThreadPageDto>.Success(new FollowedThreadPageDto(items, next));
    }
}

public class ListFollowedThreadsQueryValidator : AbstractValidator<ListFollowedThreadsQuery>
{
    public ListFollowedThreadsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.PageSizeMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.PageSizeMax}.");
    }
}
