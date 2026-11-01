using System.Buffers.Text;
using System.Text;

namespace Notrelix.Application.Features.Workspaces.Abstractions;

/// <summary>
/// Opaque keyset cursor over (name, id) for the Workspaces lists that read in name order (LD-06; default 50,
/// at most 200): base64url of <c>"&lt;id-N&gt;:&lt;name&gt;"</c>.
/// </summary>
internal static class NameCursor
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static string Encode(string name, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{id:N}:{name}"));

    public static bool TryDecode(string? value, out (string Name, Guid Id) cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value) || value.Length > 512)
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

        if (raw.Length < 33 || raw[32] != ':' || !Guid.TryParseExact(raw[..32], "N", out var id))
            return false;

        cursor = (raw[33..], id);
        return true;
    }
}
