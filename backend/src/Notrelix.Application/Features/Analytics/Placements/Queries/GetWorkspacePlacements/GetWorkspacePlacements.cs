using System.Buffers.Text;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements;

/// <summary>
/// Read model for one Analytics placement row.
/// </summary>
public sealed record WorkItemPlacementDto(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid GroupId,
    bool IsArchived,
    long SourceRevision,
    DateTimeOffset LastOccurredAt);

/// <summary>
/// Analytics-owned read over the local placement projection, as one cursor page (default 50, at most 200, in item
/// id order). It reads only the derived Analytics state; no Work Management source query occurs on the normal read
/// path. The rows are limited to the Boards the caller may read (the same Governance visibility the reports use),
/// so a Workspace member never learns the placement of an item on a Board hidden from them. Pass
/// <c>NextCursor</c> back as <c>Cursor</c> for the next page.
/// </summary>
public record GetWorkspacePlacementsQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<WorkItemPlacementDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource =>
        ResourceRef.Create(ResourceKind.Create("analytics.placement"), WorkspaceId, WorkspaceId);
}

public class GetWorkspacePlacementsQueryHandler
    : IRequestHandler<GetWorkspacePlacementsQuery, Result<CursorPage<WorkItemPlacementDto>>>
{
    private readonly IReportingDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspacePlacementsQueryHandler(
        IReportingDbContext context,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<WorkItemPlacementDto>>> Handle(
        GetWorkspacePlacementsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId;
        var boards = (await _visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, cancellationToken))
            .Concat(await _visibility.VisibleArchivedBoardIdsAsync(request.WorkspaceId, userId, cancellationToken))
            .Distinct()
            .ToList();

        var limit = request.Limit ?? GetWorkspacePlacementsQuery.DefaultLimit;
        var query = _context.WorkspaceWorkItemPlacements
            .AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId && boards.Contains(p.BoardId));

        if (PlacementCursor.TryDecode(request.Cursor, out var afterItemId))
            query = query.Where(p => p.ItemId.CompareTo(afterItemId) > 0);

        var rows = await query
            .OrderBy(p => p.ItemId)
            .Take(limit + 1)
            .Select(p => new WorkItemPlacementDto(
                p.WorkspaceId,
                p.ItemId,
                p.BoardId,
                p.GroupId,
                p.IsArchived,
                p.SourceRevision,
                p.LastOccurredAt))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? PlacementCursor.Encode(page[^1].ItemId) : null;

        return Result<CursorPage<WorkItemPlacementDto>>.Success(new CursorPage<WorkItemPlacementDto>(page, nextCursor));
    }
}

public class GetWorkspacePlacementsQueryValidator : AbstractValidator<GetWorkspacePlacementsQuery>
{
    public GetWorkspacePlacementsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetWorkspacePlacementsQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => PlacementCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid placement list cursor.");
    }
}

/// <summary>
/// Opaque keyset cursor over the item id (unique per Workspace): base64url of the id in "N" form.
/// </summary>
internal static class PlacementCursor
{
    public static string Encode(Guid itemId) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(itemId.ToString("N")));

    public static bool TryDecode(string? value, out Guid itemId)
    {
        itemId = default;
        if (string.IsNullOrEmpty(value) || value.Length > 64)
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

        return Guid.TryParseExact(raw, "N", out itemId);
    }
}
