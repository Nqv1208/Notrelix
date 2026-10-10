using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;

/// <summary>
/// WX-BE-005 "My Work": open Items assigned to the current user across the
/// Workspace's Boards, as a bounded cursor page. Items are ordered by due date
/// (earliest first, undated last, ties by id) so the client can group the page
/// into due windows (overdue / today / this week / later / no date) without
/// reloading. Completed, archived and deleted Items, and Items on archived or
/// deleted Boards, are excluded.
/// </summary>
public record GetMyBoardItemsQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<MyWorkItemDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public record MyWorkItemDto(
    Guid Id,
    Guid BoardId,
    Guid GroupId,
    string Name,
    DateTimeOffset? DueAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? UpdatedAt,
    long Version);

public class GetMyBoardItemsQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext) : IRequestHandler<GetMyBoardItemsQuery, Result<CursorPage<MyWorkItemDto>>>
{
    public async Task<Result<CursorPage<MyWorkItemDto>>> Handle(GetMyBoardItemsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var limit = request.Limit ?? GetMyBoardItemsQuery.DefaultLimit;

        var query =
            from member in context.BoardItemMembers.AsNoTracking()
            join item in context.BoardItems.AsNoTracking() on member.ItemId equals item.Id
            join board in context.Boards.AsNoTracking() on item.BoardId equals board.Id
            where member.UserId == userId
                  && member.WorkspaceId == request.WorkspaceId
                  && item.WorkspaceId == request.WorkspaceId
                  && board.WorkspaceId == request.WorkspaceId
                  && item.DeletedAt == null
                  && !item.IsArchived
                  && item.CompletedAt == null
                  && board.DeletedAt == null
                  && !board.IsArchived
            select item;

        if (MyWorkCursor.TryDecode(request.Cursor, out var cursor))
        {
            if (cursor.DueAt is { } cursorDue)
            {
                var cursorId = cursor.Id;
                query = query.Where(i =>
                    i.DueAt == null
                    || i.DueAt > cursorDue
                    || (i.DueAt == cursorDue && i.Id.CompareTo(cursorId) > 0));
            }
            else
            {
                var cursorId = cursor.Id;
                query = query.Where(i => i.DueAt == null && i.Id.CompareTo(cursorId) > 0);
            }
        }

        var rows = await query
            .OrderBy(i => i.DueAt == null)
            .ThenBy(i => i.DueAt)
            .ThenBy(i => i.Id)
            .Take(limit + 1)
            .Select(i => new MyWorkItemDto(
                i.Id,
                i.BoardId,
                i.GroupId,
                i.Name,
                i.DueAt,
                i.StartedAt,
                i.UpdatedAt,
                i.Version))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? MyWorkCursor.Encode(page[^1].DueAt, page[^1].Id) : null;

        return Result<CursorPage<MyWorkItemDto>>.Success(new CursorPage<MyWorkItemDto>(page, nextCursor));
    }
}

public class GetMyBoardItemsQueryValidator : AbstractValidator<GetMyBoardItemsQuery>
{
    public GetMyBoardItemsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetMyBoardItemsQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => MyWorkCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid My Work cursor.");
    }
}

/// <summary>
/// Opaque keyset cursor over (due date, id): base64url of
/// <c>"&lt;due-utc-ticks or n&gt;:&lt;id-N&gt;"</c>.
/// </summary>
internal readonly record struct MyWorkCursor(DateTimeOffset? DueAt, Guid Id)
{
    public static string Encode(DateTimeOffset? dueAt, Guid id)
    {
        var due = dueAt.HasValue
            ? dueAt.Value.UtcTicks.ToString(CultureInfo.InvariantCulture)
            : "n";
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{due}:{id:N}"));
    }

    public static bool TryDecode(string? value, out MyWorkCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value) || value.Length > 128)
            return false;

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = raw.Split(':');
        if (parts.Length != 2 || !Guid.TryParseExact(parts[1], "N", out var id))
            return false;

        if (parts[0] == "n")
        {
            cursor = new MyWorkCursor(null, id);
            return true;
        }

        if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks)
        {
            return false;
        }

        cursor = new MyWorkCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
