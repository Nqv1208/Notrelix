using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Notrelix.Application.Features.Analytics.Dashboards.Services;

/// <summary>Opaque keyset cursor over (created-at, id), newest first: base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class DashboardCursor
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
