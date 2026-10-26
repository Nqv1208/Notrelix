namespace Notrelix.Application.Features.Identity.Profiles.Services;

/// <summary>A profile photo format recognised from the bytes: its content type and the object-key extension.</summary>
public readonly record struct AvatarFormat(string ContentType, string Extension);

/// <summary>
/// The one policy for profile photos: only PNG, JPEG and WebP raster images, recognised from the
/// leading bytes (never from a client-declared content type or file name), up to
/// <see cref="MaxBytes"/>. Anything else, including SVG and GIF, is refused.
/// </summary>
public static class AvatarImage
{
    /// <summary>Hard cap on the stored photo, enforced while the body is streamed. The web client pre-checks 1,500,000 bytes.</summary>
    public const long MaxBytes = 2_000_000;

    /// <summary>Number of leading bytes the handler reads before classifying.</summary>
    public const int SniffLength = 16;

    public static AvatarFormat? Classify(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 8 && head[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return new AvatarFormat("image/png", "png");
        }

        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return new AvatarFormat("image/jpeg", "jpg");
        }

        // RIFF <size> WEBP then a VP8 / VP8L / VP8X chunk.
        if (head.Length >= 16
            && head[..4].SequenceEqual("RIFF"u8)
            && head.Slice(8, 4).SequenceEqual("WEBP"u8)
            && (head.Slice(12, 4).SequenceEqual("VP8 "u8)
                || head.Slice(12, 4).SequenceEqual("VP8L"u8)
                || head.Slice(12, 4).SequenceEqual("VP8X"u8)))
        {
            return new AvatarFormat("image/webp", "webp");
        }

        return null;
    }
}
