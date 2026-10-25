using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetWorkspaceInbox;

/// <summary>
/// The caller's own notifications in a Workspace, newest first, one page at a time. A notification about an
/// Item on a Board the caller can no longer read is not shown (COL-023: a historical notification never keeps
/// access alive). Each request scans at most <see cref="ScanLimit"/> notifications after the cursor and answers
/// at most <see cref="Limit"/> readable ones; <c>NextCursor</c> continues the listing, null when it ended.
/// </summary>
public record GetWorkspaceInboxQuery(Guid WorkspaceId, bool UnreadOnly, string? Cursor, int? Limit)
    : IQuery<Result<WorkspaceInboxDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;
    public const int ScanLimit = 100;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed record InboxPersonDto(Guid Id, string Name);

public sealed record InboxTargetDto(string Kind, Guid Id, Guid? BoardId, string? Key, string? Title);

public sealed record InboxEntryDto(
    Guid Id,
    string Type,
    DateTimeOffset CreatedAt,
    bool Unread,
    InboxPersonDto? Actor,
    string? Excerpt,
    InboxTargetDto? Target);

public sealed record WorkspaceInboxDto(IReadOnlyList<InboxEntryDto> Items, string? NextCursor);

public class GetWorkspaceInboxQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IResourceVisibility visibility,
    INotificationInbox inbox,
    IActorLookupService actors) : IRequestHandler<GetWorkspaceInboxQuery, Result<WorkspaceInboxDto>>
{
    private const string BoardItemKind = "work-management.board-item";

    public async Task<Result<WorkspaceInboxDto>> Handle(GetWorkspaceInboxQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var limit = Math.Clamp(request.Limit ?? GetWorkspaceInboxQuery.DefaultLimit, 1, GetWorkspaceInboxQuery.MaxLimit);
        if (!TryDecode(request.Cursor, out var beforeAt, out var beforeId))
        {
            return Result<WorkspaceInboxDto>.Failure("The cursor is not one this inbox issued.");
        }

        var scanned = await inbox.ListAsync(
            request.WorkspaceId, userId, request.UnreadOnly, beforeAt, beforeId,
            GetWorkspaceInboxQuery.ScanLimit, cancellationToken);
        if (scanned.Count == 0)
        {
            return Result<WorkspaceInboxDto>.Success(new WorkspaceInboxDto([], null));
        }

        var itemIds = scanned
            .Where(n => n.ResourceKind == BoardItemKind && n.ResourceId is not null)
            .Select(n => n.ResourceId!.Value)
            .Distinct()
            .ToList();
        var readable = new Dictionary<Guid, (Guid BoardId, string? Key, string Name)>();
        if (itemIds.Count > 0)
        {
            var visibleBoardIds = await visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, cancellationToken);
            if (visibleBoardIds.Count > 0)
            {
                readable = (await context.BoardItems
                        .AsNoTracking()
                        .Where(i => itemIds.Contains(i.Id)
                                    && i.WorkspaceId == request.WorkspaceId
                                    && i.DeletedAt == null
                                    && visibleBoardIds.Contains(i.BoardId))
                        .Select(i => new { i.Id, i.BoardId, i.ItemKey, i.Name })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(i => i.Id, i => (i.BoardId, i.ItemKey, i.Name));
            }
        }

        // A notification without a target, or about an Item the caller reads, is shown; about one they do not read, not.
        var shown = scanned
            .Where(n => n.ResourceKind != BoardItemKind || n.ResourceId is null || readable.ContainsKey(n.ResourceId.Value))
            .ToList();
        var page = shown.Take(limit).ToList();

        var actorIds = page.Select(n => n.ActorUserId).OfType<Guid>().Distinct().ToList();
        var people = (await actors.FindManyAsync(actorIds, cancellationToken)).ToDictionary(a => a.UserId, a => a.Name);

        var items = page.Select(n =>
        {
            InboxTargetDto? target = null;
            if (n.ResourceKind == BoardItemKind && n.ResourceId is { } itemId && readable.TryGetValue(itemId, out var item))
            {
                target = new InboxTargetDto("item", itemId, item.BoardId, item.Key, item.Name);
            }

            return new InboxEntryDto(
                n.NotificationId,
                n.NotificationType,
                n.CreatedAt,
                n.Unread,
                n.ActorUserId is { } actorId ? new InboxPersonDto(actorId, people.GetValueOrDefault(actorId, string.Empty)) : null,
                n.Body,
                target);
        }).ToList();

        // More may follow when readable ones were left over, or when the scan was full.
        var last = shown.Count > limit ? page[^1] : scanned.Count == GetWorkspaceInboxQuery.ScanLimit ? scanned[^1] : null;
        return Result<WorkspaceInboxDto>.Success(new WorkspaceInboxDto(items, last is null ? null : Encode(last)));
    }

    private static string Encode(InboxNotification last) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{last.CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}|{last.NotificationId:N}"))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static bool TryDecode(string? cursor, out DateTimeOffset? at, out Guid? id)
    {
        at = null;
        id = null;
        if (string.IsNullOrEmpty(cursor)) return true;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(padded)).Split('|');
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                || !Guid.TryParseExact(parts[1], "N", out var parsedId)
                || ticks > DateTimeOffset.MaxValue.UtcTicks)
            {
                return false;
            }

            at = new DateTimeOffset(ticks, TimeSpan.Zero);
            id = parsedId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public class GetWorkspaceInboxQueryValidator : AbstractValidator<GetWorkspaceInboxQuery>
{
    public GetWorkspaceInboxQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, GetWorkspaceInboxQuery.MaxLimit).When(x => x.Limit is not null);
        RuleFor(x => x.Cursor).MaximumLength(200);
    }
}
