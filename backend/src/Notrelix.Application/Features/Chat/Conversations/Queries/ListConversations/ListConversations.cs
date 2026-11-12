using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Conversations.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Conversations.Queries.ListConversations;

/// <summary>
/// The direct and group conversations the caller takes part in, newest activity first (UC-CHT-E05). A conversation the caller hid
/// (UC-CHT-E07) is left out. Each one carries its participants, the newest message as a preview, and the caller's unread count.
/// Limit is 1 to 100 (default 50); the list is not paged beyond it.
/// </summary>
public record ListConversationsQuery(Guid WorkspaceId, int Limit = 50)
    : IQuery<Result<IReadOnlyList<ConversationDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListConversationsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<ListConversationsQuery, Result<IReadOnlyList<ConversationDto>>>
{
    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(ListConversationsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var mine = db.ChatMemberships
            .Where(m => m.UserId == userId && m.WorkspaceId == request.WorkspaceId && m.LeftAt == null && m.HiddenAt == null)
            .Select(m => m.SpaceId);
        var spaces = await db.ChatSpaces
            .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null
                && (s.Kind == SpaceKind.Direct || s.Kind == SpaceKind.Group)
                && mine.Contains(s.Id))
            .ToListAsync(cancellationToken);
        if (spaces.Count == 0)
            return Result<IReadOnlyList<ConversationDto>>.Success([]);

        var spaceIds = spaces.Select(s => s.Id).ToArray();
        // Participants of every listed conversation load onto their spaces (the fix-up of the tracked navigation).
        await db.ChatMemberships.Where(m => spaceIds.Contains(m.SpaceId) && m.LeftAt == null).ToListAsync(cancellationToken);

        var newest = db.ChatMessages.AsNoTracking()
            .Where(m => spaceIds.Contains(m.SpaceId))
            .GroupBy(m => m.SpaceId)
            .Select(g => new { SpaceId = g.Key, Sequence = g.Max(x => x.Sequence) });
        var previews = await db.ChatMessages.AsNoTracking()
            .Join(newest,
                m => new { m.SpaceId, m.Sequence },
                n => new { n.SpaceId, n.Sequence },
                (m, n) => new { m.SpaceId, m.Sequence, m.AuthorId, m.Kind, m.Content, m.DeletedAt, m.CreatedAt })
            .ToListAsync(cancellationToken);
        var previewBySpace = previews.ToDictionary(p => p.SpaceId);

        var unread = await UnreadSummaryBuilder.BuildAsync(db, request.WorkspaceId, userId, clock.UtcNow, cancellationToken);
        var unreadBySpace = unread.Spaces.ToDictionary(s => s.SpaceId, s => s.UnreadCount);

        var rows = spaces.Select(space =>
        {
            ConversationPreviewDto? preview = null;
            if (previewBySpace.TryGetValue(space.Id, out var last))
            {
                preview = ConversationDtoBuilder.Preview(last.Sequence, last.AuthorId, last.Kind, last.Content, last.DeletedAt is not null, last.CreatedAt);
            }
            var dto = ConversationDtoBuilder.From(
                space,
                space.GetActiveMembership(userId),
                unreadBySpace.GetValueOrDefault(space.Id),
                preview);
            return dto;
        })
        .OrderByDescending(c => c.LastActivityAt)
        .ThenBy(c => c.Id)
        .Take(Math.Clamp(request.Limit, 1, 100))
        .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(rows);
    }
}

public class ListConversationsQueryValidator : AbstractValidator<ListConversationsQuery>
{
    public ListConversationsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
