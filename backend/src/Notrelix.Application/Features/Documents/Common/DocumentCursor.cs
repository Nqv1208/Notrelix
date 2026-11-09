using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Notrelix.Application.Features.Documents.Common;

/// <summary>
/// The opaque keyset cursors of the Documents lists that page by an id or by a version number. A cursor names
/// the last row of the page before, so a row added or removed elsewhere in the list cannot shift a page.
/// </summary>
internal static class DocumentCursor
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static int ClampLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

    public static string EncodeId(Guid id) => Encode($"id:{id:N}");

    public static bool TryDecodeId(string? value, out Guid id)
    {
        id = default;
        return TryRaw(value, out var raw) && raw.StartsWith("id:", StringComparison.Ordinal)
            && Guid.TryParseExact(raw["id:".Length..], "N", out id);
    }

    public static string EncodeNumber(int number) => Encode($"n:{number.ToString(CultureInfo.InvariantCulture)}");

    public static bool TryDecodeNumber(string? value, out int number)
    {
        number = default;
        return TryRaw(value, out var raw) && raw.StartsWith("n:", StringComparison.Ordinal)
            && int.TryParse(raw["n:".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out number)
            && number > 0;
    }

    private static string Encode(string raw) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));

    private static bool TryRaw(string? value, out string raw)
    {
        raw = string.Empty;
        if (string.IsNullOrEmpty(value) || value.Length > 128) return false;
        try
        {
            raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
