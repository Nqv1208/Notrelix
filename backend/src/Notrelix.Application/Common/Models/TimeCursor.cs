using FluentValidation.Results;

namespace Notrelix.Application.Common.Models;

/// <summary>Where a time-ordered page stopped: the time and id of its last row.</summary>
public sealed record TimeCursorPosition(DateTimeOffset At, Guid Id);

/// <summary>
/// An opaque cursor for a list ordered by a time and then the id: the next page is what follows the last row of this
/// one, so a row added or removed elsewhere in the list cannot shift a page.
/// </summary>
public static class TimeCursor
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static int ClampLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

    public static string Encode(DateTimeOffset at, Guid id) =>
        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"t1:{at.UtcTicks}:{id:N}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static TimeCursorPosition? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var parts = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(padded)).Split(':');
            if (parts is ["t1", var ticks, var id] && long.TryParse(ticks, out var utcTicks) && Guid.TryParseExact(id, "N", out var guid)
                && utcTicks >= 0 && utcTicks <= DateTime.MaxValue.Ticks)
            {
                return new TimeCursorPosition(new DateTimeOffset(utcTicks, TimeSpan.Zero), guid);
            }
        }
        catch (FormatException) { }

        throw new Notrelix.Application.Common.Exceptions.ValidationException([new ValidationFailure("cursor", "The cursor is not valid.")]);
    }

    /// <summary>The page of <paramref name="rows"/> read with one more than <paramref name="limit"/>: the rows to show and the cursor of the next page.</summary>
    public static CursorPage<T> Page<T>(IReadOnlyList<T> rows, int limit, Func<T, (DateTimeOffset At, Guid Id)> position)
    {
        var more = rows.Count > limit;
        var kept = more ? rows.Take(limit).ToList() : rows;
        string? next = null;
        if (more)
        {
            var (at, id) = position(kept[^1]);
            next = Encode(at, id);
        }

        return new CursorPage<T>(kept, next);
    }
}
