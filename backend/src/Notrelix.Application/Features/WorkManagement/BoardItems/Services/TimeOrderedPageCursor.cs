namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// An opaque cursor for a list ordered newest first by one timestamp and then by id (the archive and the trash):
/// the position of the last row of a page, so the next page is what follows it and a write on another row cannot shift a page.
/// </summary>
public static class TimeOrderedPageCursor
{
    private sealed record Wire(int V, long T, Guid I);

    public static string Encode(DateTimeOffset at, Guid id) =>
        Convert.ToBase64String(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new Wire(1, at.UtcTicks, id)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static (DateTimeOffset At, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return null;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var wire = System.Text.Json.JsonSerializer.Deserialize<Wire>(Convert.FromBase64String(padded));
            if (wire is { V: 1 }) return (new DateTimeOffset(wire.T, TimeSpan.Zero), wire.I);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException or ArgumentOutOfRangeException) { }

        throw BoardItemCriteria.Refuse("cursor", "The cursor is not valid.");
    }
}
