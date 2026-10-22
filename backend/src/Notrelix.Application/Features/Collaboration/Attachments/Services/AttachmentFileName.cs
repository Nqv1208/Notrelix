using System.Globalization;
using System.Text;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// Sanitises a client-supplied file name for display and Content-Disposition. The result is
/// metadata only: it never takes part in the storage key and never reaches the file system.
/// </summary>
public static class AttachmentFileName
{
    public const int MaxLength = 255;
    private const int MaxExtensionLength = 16;
    private const string Fallback = "file";

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static string Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Fallback;
        }

        // Keep only the last path segment, whichever separator the client platform uses.
        var lastSeparator = raw.LastIndexOfAny(['/', '\\']);
        var name = lastSeparator >= 0 ? raw[(lastSeparator + 1)..] : raw;

        name = name.Normalize(NormalizationForm.FormC);

        var builder = new StringBuilder(name.Length);
        foreach (var rune in name.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);

            // Control, format (bidi overrides, zero-width), unassigned/private-use and
            // separator-line characters are dropped; shell- and file-system-hostile
            // punctuation is replaced.
            if (category is UnicodeCategory.Control
                or UnicodeCategory.Format
                or UnicodeCategory.OtherNotAssigned
                or UnicodeCategory.PrivateUse
                or UnicodeCategory.Surrogate
                or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator)
            {
                continue;
            }

            if (rune.Value is '<' or '>' or ':' or '"' or '|' or '?' or '*')
            {
                builder.Append('_');
                continue;
            }

            builder.Append(rune.ToString());
        }

        var cleaned = builder.ToString().Trim().TrimEnd('.', ' ').TrimStart('.', ' ');
        if (cleaned.Length == 0)
        {
            return Fallback;
        }

        cleaned = Truncate(cleaned);

        var stem = Path.GetFileNameWithoutExtension(cleaned);
        if (ReservedDeviceNames.Contains(stem))
        {
            cleaned = "_" + cleaned;
        }

        return cleaned;
    }

    /// <summary>Lower-case extension without the dot, or empty.</summary>
    public static string ExtensionOf(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Length > 1 ? extension[1..].ToLowerInvariant() : string.Empty;
    }

    private static string Truncate(string name)
    {
        if (name.Length <= MaxLength)
        {
            return name;
        }

        var extension = Path.GetExtension(name);
        if (extension.Length is 0 or > MaxExtensionLength + 1)
        {
            return name[..MaxLength];
        }

        var stemLength = MaxLength - extension.Length;
        return name[..stemLength] + extension;
    }
}
