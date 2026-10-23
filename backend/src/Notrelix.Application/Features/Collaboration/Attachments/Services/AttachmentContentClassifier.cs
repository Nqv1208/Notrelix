using System.Text;
using System.Text.Unicode;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

public readonly record struct ClassifiedContent(string ContentType, AttachmentType Type);

/// <summary>
/// Derives the content type of an uploaded file from its leading bytes (magic numbers), never
/// from the client-declared Content-Type. The file extension is consulted only to refine
/// container formats (ZIP-based Office files, OLE2 legacy Office files, plain text) and the
/// result is used for labelling; downloads of non-image types are always forced to attachment.
/// </summary>
public static class AttachmentContentClassifier
{
    /// <summary>Number of leading bytes the handler reads before classifying.</summary>
    public const int SniffLength = 512;

    public const string OctetStream = "application/octet-stream";

    public static ClassifiedContent Classify(ReadOnlySpan<byte> head, string fileName)
    {
        var extension = AttachmentFileName.ExtensionOf(fileName);

        if (StartsWith(head, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return new("image/png", AttachmentType.Image);
        if (StartsWith(head, 0xFF, 0xD8, 0xFF)) return new("image/jpeg", AttachmentType.Image);
        if (StartsWithAscii(head, 0, "GIF87a") || StartsWithAscii(head, 0, "GIF89a")) return new("image/gif", AttachmentType.Image);
        if (StartsWithAscii(head, 0, "RIFF") && StartsWithAscii(head, 8, "WEBP")) return new("image/webp", AttachmentType.Image);
        if (StartsWithAscii(head, 0, "%PDF-")) return new("application/pdf", AttachmentType.PDF);

        if (StartsWith(head, 0x50, 0x4B, 0x03, 0x04) || StartsWith(head, 0x50, 0x4B, 0x05, 0x06))
        {
            return extension switch
            {
                "docx" => new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", AttachmentType.Document),
                "xlsx" => new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", AttachmentType.Spreadsheet),
                "pptx" => new("application/vnd.openxmlformats-officedocument.presentationml.presentation", AttachmentType.Document),
                _ => new("application/zip", AttachmentType.Archive),
            };
        }

        if (StartsWith(head, 0x1F, 0x8B)) return new("application/gzip", AttachmentType.Archive);
        if (StartsWith(head, 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C)) return new("application/x-7z-compressed", AttachmentType.Archive);
        if (StartsWithAscii(head, 0, "Rar!")) return new("application/vnd.rar", AttachmentType.Archive);
        if (StartsWith(head, 0x1A, 0x45, 0xDF, 0xA3)) return new("video/webm", AttachmentType.Video);
        if (StartsWithAscii(head, 4, "ftyp"))
        {
            var brand = head.Length >= 12 ? Encoding.ASCII.GetString(head.Slice(8, 4)) : string.Empty;
            return brand is "heic" or "heix" or "mif1" or "msf1"
                ? new("image/heif", AttachmentType.Image)
                : new("video/mp4", AttachmentType.Video);
        }

        if (StartsWith(head, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1))
        {
            return extension switch
            {
                "doc" => new("application/msword", AttachmentType.Document),
                "xls" => new("application/vnd.ms-excel", AttachmentType.Spreadsheet),
                "ppt" => new("application/vnd.ms-powerpoint", AttachmentType.Document),
                _ => new(OctetStream, AttachmentType.Document),
            };
        }

        if (LooksLikeText(head))
        {
            return extension == "csv"
                ? new("text/csv", AttachmentType.Spreadsheet)
                : new("text/plain", AttachmentType.Document);
        }

        return new(OctetStream, AttachmentType.Document);
    }

    /// <summary>Executable or script signatures (PE, ELF, Mach-O, Java class, shebang).</summary>
    public static bool LooksExecutable(ReadOnlySpan<byte> head) =>
        StartsWith(head, 0x4D, 0x5A)
        || StartsWith(head, 0x7F, 0x45, 0x4C, 0x46)
        || StartsWith(head, 0xCA, 0xFE, 0xBA, 0xBE)
        || StartsWith(head, 0xFE, 0xED, 0xFA, 0xCE)
        || StartsWith(head, 0xFE, 0xED, 0xFA, 0xCF)
        || StartsWith(head, 0xCE, 0xFA, 0xED, 0xFE)
        || StartsWith(head, 0xCF, 0xFA, 0xED, 0xFE)
        || StartsWithAscii(head, 0, "#!");

    public static bool IsBlockedExtension(string fileName, IEnumerable<string> blockedExtensions)
    {
        var extension = AttachmentFileName.ExtensionOf(fileName);
        return extension.Length > 0 && blockedExtensions.Contains(extension, StringComparer.Ordinal);
    }

    /// <summary>Inline rendering is allowed only for sniffed raster images (never SVG, never client-declared types).</summary>
    public static bool IsInlineSafe(string contentType) =>
        contentType is "image/png" or "image/jpeg" or "image/gif" or "image/webp";

    private static bool LooksLikeText(ReadOnlySpan<byte> head)
    {
        if (head.IsEmpty || head.IndexOf((byte)0) >= 0)
        {
            return false;
        }

        // Tolerate a multi-byte sequence cut at the end of the sniffed window.
        for (var trim = 0; trim <= 3 && trim < head.Length; trim++)
        {
            if (Utf8.IsValid(head[..^trim]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWith(ReadOnlySpan<byte> head, params byte[] signature) =>
        head.Length >= signature.Length && head[..signature.Length].SequenceEqual(signature);

    private static bool StartsWithAscii(ReadOnlySpan<byte> head, int offset, string ascii)
    {
        if (head.Length < offset + ascii.Length)
        {
            return false;
        }

        for (var i = 0; i < ascii.Length; i++)
        {
            if (head[offset + i] != (byte)ascii[i])
            {
                return false;
            }
        }

        return true;
    }
}
