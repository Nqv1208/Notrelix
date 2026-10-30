using System.Buffers.Text;
using System.Globalization;
using System.Text;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

/// <summary>
/// The pages in the trash of one Workspace that the caller may see, newest change first, as a bounded
/// cursor page. A page that went to the trash with its parent is listed like any other.
/// </summary>
public record GetTrashedPagesQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<PageDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class GetTrashedPagesQueryHandler : IRequestHandler<GetTrashedPagesQuery, Result<CursorPage<PageDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetTrashedPagesQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<PageDto>>> Handle(GetTrashedPagesQuery request, CancellationToken ct)
    {
        // Same per-page ViewPage decision as the page list, applied to the trash.
        var visible = await _visibility.VisibleTrashedPageIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<CursorPage<PageDto>>.Success(new CursorPage<PageDto>([], null));

        var limit = request.Limit ?? GetTrashedPagesQuery.DefaultLimit;
        var query = _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.DeletedAt == null && page.Status == PageStatus.Archived)
            .Where(page => visible.Contains(page.Id));

        if (TrashCursor.TryDecode(request.Cursor, out var at, out var id))
            query = query.Where(page => (page.UpdatedAt ?? page.CreatedAt) < at
                || ((page.UpdatedAt ?? page.CreatedAt) == at && page.Id.CompareTo(id) < 0));

        var rows = await query
            .OrderByDescending(page => page.UpdatedAt ?? page.CreatedAt)
            .ThenByDescending(page => page.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var pages = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? TrashCursor.Encode(pages[^1].UpdatedAt ?? pages[^1].CreatedAt, pages[^1].Id) : null;

        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).ToHashSet();
        return Result<CursorPage<PageDto>>.Success(new CursorPage<PageDto>(pages.Select(page => DocumentDtoMapper.ToPageDto(page, spaces)), nextCursor));
    }
}

/// <summary>Opaque keyset cursor over (last change, id): base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class TrashCursor
{
    public static string Encode(DateTimeOffset at, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{at.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryDecode(string? value, out DateTimeOffset at, out Guid id)
    {
        at = default;
        id = default;
        if (string.IsNullOrEmpty(value) || value.Length > 128) return false;

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
        if (parts.Length != 2
            || !Guid.TryParseExact(parts[1], "N", out id)
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks)
        {
            return false;
        }

        at = new DateTimeOffset(ticks, TimeSpan.Zero);
        return true;
    }
}
