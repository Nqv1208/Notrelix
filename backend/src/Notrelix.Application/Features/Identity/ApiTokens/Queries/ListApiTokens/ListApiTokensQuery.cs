using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;

/// <summary>
/// Lists API token metadata for a workspace, newest first, as a bounded cursor page (default 50, max 200).
/// The raw secret is never returned: it exists exactly once at issuance and cannot be recovered from the digest.
/// </summary>
public sealed record ListApiTokensQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<ApiTokenSummaryDto>>>, IReadRequest, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public PermissionAction Action => PermissionAction.ManageApiTokens;
    public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class ListApiTokensQueryHandler
    : IRequestHandler<ListApiTokensQuery, Result<CursorPage<ApiTokenSummaryDto>>>
{
    private readonly IIdentityDbContext _context;

    public ListApiTokensQueryHandler(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CursorPage<ApiTokenSummaryDto>>> Handle(
        ListApiTokensQuery request, CancellationToken ct)
    {
        var limit = request.Limit ?? ListApiTokensQuery.DefaultLimit;

        var tokens = await _context.ApiTokens
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId)
            .ToListAsync(ct);

        // Newest first, with the id as the tie-break so the order is total and the cursor is exact.
        // The keyset test compares the same (created-at, id) pair the order uses, so a cursor stays valid
        // even if the token it was cut from is later revoked.
        IEnumerable<ApiToken> ordered = tokens
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id);

        if (ApiTokenCursor.TryDecode(request.Cursor, out var after))
        {
            ordered = tokens
                .Where(t => t.CreatedAt < after.CreatedAt
                            || (t.CreatedAt == after.CreatedAt && t.Id.CompareTo(after.Id) < 0))
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id);
        }

        var rows = ordered.Take(limit + 1).ToList();
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).Select(t => new ApiTokenSummaryDto(
            t.Id,
            t.Name,
            t.Scopes?.ToJson(),
            t.Status.ToString(),
            t.LastUsedAt,
            t.ExpiresAt,
            t.CreatedAt,
            t.RevokedAt)).ToList();

        var nextCursor = hasMore ? ApiTokenCursor.Encode(rows[limit - 1].CreatedAt, rows[limit - 1].Id) : null;

        return Result<CursorPage<ApiTokenSummaryDto>>.Success(new CursorPage<ApiTokenSummaryDto>(page, nextCursor));
    }
}

public sealed class ListApiTokensQueryValidator : AbstractValidator<ListApiTokensQuery>
{
    public ListApiTokensQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ListApiTokensQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => ApiTokenCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid API token cursor.");
    }
}

/// <summary>Opaque keyset cursor over (created-at, id): base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class ApiTokenCursor
{
    public static string Encode(DateTimeOffset createdAt, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{createdAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryDecode(string? value, out (DateTimeOffset CreatedAt, Guid Id) position)
    {
        position = default;
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
        if (parts.Length != 2
            || !Guid.TryParseExact(parts[1], "N", out var id)
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks)
        {
            return false;
        }

        position = (new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
