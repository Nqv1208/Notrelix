using System.Globalization;
using System.Text.RegularExpressions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Search;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Chat.Search.DTOs;
using Notrelix.Application.Features.Chat.Search.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Search.Queries.SearchChatMessages;

/// <summary>
/// Searches the messages the caller may read now (UC-CHT-H07, CHT-022, CHT-004). The text is matched as a phrase, ignoring case and Vietnamese
/// marks ("hoa don" finds "Hoá đơn"), inside the live spaces from <see cref="ChannelAccess.VisibleSpaces"/>, the same rule the history
/// handlers apply, in the same statement: there is no separate index, so a removal, a delete or a purge takes effect at once and a hit never
/// outlives the caller's access. Filters: one space, one author, a date range (from inclusive, to exclusive), messages with a file and
/// messages that mention the caller. Newest first, by cursor. Tombstones, system messages and messages past the Workspace's retention window are
/// never returned. A space the caller cannot see gives an empty result, the same as a space with no hit.
/// </summary>
public record SearchChatMessagesQuery(
    Guid WorkspaceId,
    string Q,
    Guid? SpaceId,
    Guid? AuthorId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    bool? HasAttachment,
    bool? MentionsMe,
    string? Cursor,
    int Limit)
    : IQuery<Result<ChatSearchPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = ChatLimits.SearchPageDefault;

    public PermissionAction Action => PermissionAction.ChatSearch;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SearchChatMessagesQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    IChatRateLimiter rateLimits,
    IChatMessageAttachments attachments) : IRequestHandler<SearchChatMessagesQuery, Result<ChatSearchPageDto>>
{
    public async Task<Result<ChatSearchPageDto>> Handle(SearchChatMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        await rateLimits.RequireAsync(ChatRateLimitPolicy.Search, request.WorkspaceId, userId, cancellationToken);

        var pattern = SearchTextFolding.ToPattern(request.Q);
        if (pattern.Length == 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("q is too short.");

        var cursor = ParseCursor(request.Cursor);
        var visible = ChannelAccess.VisibleSpaces(db, request.WorkspaceId, userId, ChannelAccess.IsGuest(caller)).AsNoTracking();

        var query = db.ChatMessages.AsNoTracking().Where(m =>
            m.WorkspaceId == request.WorkspaceId
            && m.Kind == MessageKind.User
            && m.DeletedAt == null
            && m.Content != null
            && visible.Select(s => s.Id).Contains(m.SpaceId)
            && Regex.IsMatch(m.Content, pattern));

        if (request.SpaceId is { } spaceId) query = query.Where(m => m.SpaceId == spaceId);
        if (request.AuthorId is { } authorId) query = query.Where(m => m.AuthorId == authorId);
        if (request.From is { } rangeFrom) query = query.Where(m => m.CreatedAt >= rangeFrom);
        if (request.To is { } rangeTo) query = query.Where(m => m.CreatedAt < rangeTo);
        if (request.MentionsMe == true)
            query = query.Where(m => db.ChatMessageMentions.Any(x => x.MessageId == m.Id && x.Kind == MentionKind.User && x.TargetId == userId));

        // A message past the Workspace's window is gone for readers even before the retention job reaches it (UC-CHT-H05 hook).
        var window = await db.ChatRetentionPolicies.AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId)
            .Select(p => p.RetentionDays)
            .FirstOrDefaultAsync(cancellationToken);
        if (window is { } days)
        {
            var cutoff = clock.UtcNow.AddDays(-days);
            query = query.Where(m => m.CreatedAt >= cutoff);
        }

        // Files live in Collaboration, so "has a file" is asked of that boundary for each scanned batch; the scan goes on until the page is
        // full, the hits run out or the scan bound is reached (then the cursor continues from the last scanned message).
        var wantFiles = request.HasAttachment == true;
        var batchSize = wantFiles ? ScanBatch : request.Limit + 1;
        var collected = new List<Row>();
        var position = cursor;
        var exhausted = false;
        for (var scan = 0; scan < (wantFiles ? MaxScans : 1) && collected.Count <= request.Limit; scan++)
        {
            var scanQuery = query;
            if (position is { } last)
            {
                var (beforeAt, beforeId) = last;
                scanQuery = scanQuery.Where(m => m.CreatedAt < beforeAt || (m.CreatedAt == beforeAt && m.Id.CompareTo(beforeId) < 0));
            }

            var batch = await scanQuery
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Take(batchSize)
                .Select(m => new Row(m.Id, m.SpaceId, m.AuthorId, m.CreatedAt, m.Sequence, m.ThreadRootId, m.AlsoSentToChannel, m.Content!))
                .ToListAsync(cancellationToken);
            exhausted = batch.Count < batchSize;
            if (batch.Count > 0) position = (batch[^1].CreatedAt, batch[^1].Id);

            if (wantFiles && batch.Count > 0)
            {
                var withFiles = (await attachments.ListAsync(batch.Select(r => r.Id).ToList(), cancellationToken)).Keys.ToHashSet();
                batch = batch.Where(r => withFiles.Contains(r.Id)).ToList();
            }

            collected.AddRange(batch);
            if (exhausted) break;
        }

        var rows = collected;

        var page = rows.Take(request.Limit).ToList();
        var spaceIds = page.Select(r => r.SpaceId).Distinct().ToList();
        var spaces = spaceIds.Count == 0
            ? new Dictionary<Guid, Space>()
            : (await db.ChatSpaces.AsNoTracking().Where(s => spaceIds.Contains(s.Id)).ToListAsync(cancellationToken)).ToDictionary(s => s.Id);

        var items = page.Select(r =>
        {
            var space = spaces[r.SpaceId];
            return new ChatSearchHitDto(
                r.Id, r.SpaceId, space.Name, space.Kind.ToWire(), space.IsArchived, r.AuthorId, r.CreatedAt, r.Sequence,
                r.ThreadRootId, r.AlsoSentToChannel, ChatSearchSnippets.Build(r.Content, request.Q));
        }).ToList();

        string? next = null;
        if (collected.Count > request.Limit && page.Count > 0)
            next = FormatCursor(page[^1].CreatedAt, page[^1].Id);
        else if (!exhausted && position is { } scanned)
            next = FormatCursor(scanned.At, scanned.Id);
        return Result<ChatSearchPageDto>.Success(new ChatSearchPageDto(items, next));
    }

    private const int ScanBatch = 200;
    private const int MaxScans = 10;

    private sealed record Row(Guid Id, Guid SpaceId, Guid AuthorId, DateTimeOffset CreatedAt, long Sequence, Guid? ThreadRootId, bool AlsoSentToChannel, string Content);

    private static string FormatCursor(DateTimeOffset at, Guid id) => $"{at.UtcTicks.ToString(CultureInfo.InvariantCulture)}.{id:N}";

    private static (DateTimeOffset At, Guid Id)? ParseCursor(string? cursor)
    {
        if (cursor is null) return null;
        var parts = cursor.Split('.');
        if (parts.Length == 2
            && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            && ticks <= DateTimeOffset.MaxValue.Ticks
            && Guid.TryParseExact(parts[1], "N", out var id))
            return (new DateTimeOffset(ticks, TimeSpan.Zero), id);

        throw new Notrelix.Application.Common.Exceptions.ValidationException("cursor is not valid.");
    }
}

public class SearchChatMessagesQueryValidator : AbstractValidator<SearchChatMessagesQuery>
{
    public SearchChatMessagesQueryValidator()
    {
        RuleFor(x => x.Q)
            .Must(q => SearchTextFolding.Clean(q).Length >= SearchTextFolding.MinLength)
            .WithMessage($"q needs at least {SearchTextFolding.MinLength} characters.");
        RuleFor(x => x.Q)
            .Must(q => (q ?? string.Empty).Trim().Length <= SearchTextFolding.MaxLength)
            .WithMessage($"q is at most {SearchTextFolding.MaxLength} characters.");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.SearchPageMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.SearchPageMax}.");
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.To > x.From)
            .WithName("range")
            .WithMessage("The range must end after it starts.");
    }
}
