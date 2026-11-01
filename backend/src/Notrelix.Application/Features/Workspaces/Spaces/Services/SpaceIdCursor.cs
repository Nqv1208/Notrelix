using System.Buffers.Text;
using System.Text;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Opaque keyset cursor over a Space id, ascending (LD-06): base64url of the id in "N" form. The tree and
/// the flat list read in this order; the client sorts siblings by position itself.
/// </summary>
internal static class SpaceIdCursor
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public static string Encode(Guid id) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(id.ToString("N")));

    public static bool TryDecode(string? value, out Guid id)
    {
        id = default;
        if (string.IsNullOrEmpty(value) || value.Length > 64)
            return false;

        try
        {
            return Guid.TryParseExact(Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)), "N", out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
