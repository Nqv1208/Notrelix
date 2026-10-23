using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Security.Abstractions;

namespace Notrelix.Application.Features.Identity.Security.Queries.GetSecurityActivity;

/// <summary>
/// The caller's own security activity, newest first, as a bounded cursor page. The subject is always
/// the principal: there is no user id input, so one user can never read another user's feed.
/// </summary>
public sealed record GetSecurityActivityQuery(string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<SecurityActivityDto>>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;
}

public sealed class GetSecurityActivityQueryHandler
    : IRequestHandler<GetSecurityActivityQuery, Result<CursorPage<SecurityActivityDto>>>
{
    private readonly ISecurityActivityReader _reader;
    private readonly ICurrentRequestContext _currentUser;

    public GetSecurityActivityQueryHandler(ISecurityActivityReader reader, ICurrentRequestContext currentUser)
    {
        _reader = reader;
        _currentUser = currentUser;
    }

    public async Task<Result<CursorPage<SecurityActivityDto>>> Handle(
        GetSecurityActivityQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit ?? GetSecurityActivityQuery.DefaultLimit;

        SecurityActivityPosition? after = null;
        if (SecurityActivityCursor.TryDecode(request.Cursor, out var position))
        {
            after = position;
        }

        var rows = await _reader.ListAsync(_currentUser.UserId, after, limit + 1, cancellationToken);

        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? SecurityActivityCursor.Encode(page[^1].OccurredAt, page[^1].Id) : null;

        return Result<CursorPage<SecurityActivityDto>>.Success(new CursorPage<SecurityActivityDto>(page, nextCursor));
    }
}

public sealed class GetSecurityActivityQueryValidator : AbstractValidator<GetSecurityActivityQuery>
{
    public GetSecurityActivityQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetSecurityActivityQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => SecurityActivityCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid security activity cursor.");
    }
}

/// <summary>Opaque keyset cursor over (occurred-at, id): base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class SecurityActivityCursor
{
    public static string Encode(DateTimeOffset occurredAt, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{occurredAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryDecode(string? value, out SecurityActivityPosition position)
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

        position = new SecurityActivityPosition(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
