using System.Buffers.Binary;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>The pixel size of an image, read from its header. Both sides are within <see cref="ImageDimensionReader.MinSide"/> to <see cref="ImageDimensionReader.MaxSide"/>.</summary>
public readonly record struct ImageSize(int Width, int Height);

/// <summary>
/// Reads the pixel size of a PNG, JPEG, GIF or WebP image from its leading bytes only (no decoding, no package). The result is advisory
/// layout data: a truncated, garbage or unsupported header, or a size outside the sane range, yields null, never an error. The size is
/// always taken from the stored bytes, never from anything the client declared.
/// </summary>
public static class ImageDimensionReader
{
    public const int MinSide = 1;
    public const int MaxSide = FileMetadata.MaxImageSide;

    /// <summary>Reads the size of an image of the given sniffed content type from <paramref name="header"/>, or null.</summary>
    public static ImageSize? TryRead(ReadOnlySpan<byte> header, string contentType)
    {
        var size = contentType switch
        {
            "image/png" => Png(header),
            "image/jpeg" => Jpeg(header),
            "image/gif" => Gif(header),
            "image/webp" => WebP(header),
            _ => null,
        };

        return size is { } s && IsSane(s.Width) && IsSane(s.Height) ? s : null;
    }

    private static bool IsSane(int side) => side is >= MinSide and <= MaxSide;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // Signature (8 bytes), then the IHDR chunk: length (4), type (4), width (4 BE), height (4 BE).
    private static ImageSize? Png(ReadOnlySpan<byte> h)
    {
        if (h.Length < 24 || !h[..8].SequenceEqual(PngSignature) || !h.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            return null;
        }

        var width = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(20, 4));
        return Clamp(width, height);
    }

    // "GIF87a" or "GIF89a", then the logical screen width and height as little-endian 16-bit values.
    private static ImageSize? Gif(ReadOnlySpan<byte> h)
    {
        if (h.Length < 10 || !(h[..6].SequenceEqual("GIF87a"u8) || h[..6].SequenceEqual("GIF89a"u8)))
        {
            return null;
        }

        return Clamp(BinaryPrimitives.ReadUInt16LittleEndian(h.Slice(6, 2)), BinaryPrimitives.ReadUInt16LittleEndian(h.Slice(8, 2)));
    }

    // Walks the marker segments until a start-of-frame marker (SOF0..SOF15 except DHT, JPG and DAC). Segments before it (APPn, DQT,
    // COM and so on) are skipped by their own length, so a frame header that is not the first segment is found.
    private static ImageSize? Jpeg(ReadOnlySpan<byte> h)
    {
        if (h.Length < 4 || h[0] != 0xFF || h[1] != 0xD8)
        {
            return null;
        }

        var pos = 2;
        while (pos + 1 < h.Length)
        {
            if (h[pos] != 0xFF)
            {
                return null;
            }

            var marker = h[pos + 1];
            pos += 2;

            // Fill bytes before a marker, and markers that carry no length (SOI, EOI, RSTn, TEM).
            if (marker == 0xFF)
            {
                pos--;
                continue;
            }

            if (marker is 0x01 or 0xD8 or 0xD9 or (>= 0xD0 and <= 0xD7))
            {
                if (marker == 0xD9)
                {
                    return null;
                }

                continue;
            }

            // Start of scan before any frame header: the image has no frame header to read.
            if (marker == 0xDA || pos + 2 > h.Length)
            {
                return null;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(h.Slice(pos, 2));
            if (length < 2)
            {
                return null;
            }

            if (IsStartOfFrame(marker))
            {
                // Segment: length (2), precision (1), height (2 BE), width (2 BE).
                if (pos + 7 > h.Length)
                {
                    return null;
                }

                return Clamp(BinaryPrimitives.ReadUInt16BigEndian(h.Slice(pos + 5, 2)), BinaryPrimitives.ReadUInt16BigEndian(h.Slice(pos + 3, 2)));
            }

            pos += length;
        }

        return null;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC);

    // RIFF container: "RIFF", file size (4), "WEBP", then one chunk: VP8 (lossy), VP8L (lossless) or VP8X (extended).
    private static ImageSize? WebP(ReadOnlySpan<byte> h)
    {
        if (h.Length < 20 || !h[..4].SequenceEqual("RIFF"u8) || !h.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return null;
        }

        var chunk = h.Slice(12, 4);
        if (chunk.SequenceEqual("VP8 "u8))
        {
            // Frame tag at 20 (3 bytes), then the start code 9D 01 2A at 23, width and height as 14-bit little-endian values.
            if (h.Length < 30 || h[23] != 0x9D || h[24] != 0x01 || h[25] != 0x2A)
            {
                return null;
            }

            return Clamp(BinaryPrimitives.ReadUInt16LittleEndian(h.Slice(26, 2)) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(h.Slice(28, 2)) & 0x3FFF);
        }

        if (chunk.SequenceEqual("VP8L"u8))
        {
            // Signature byte 0x2F at 20, then 32 bits at 21: width - 1 in the low 14 bits, height - 1 in the next 14.
            if (h.Length < 25 || h[20] != 0x2F)
            {
                return null;
            }

            var bits = BinaryPrimitives.ReadUInt32LittleEndian(h.Slice(21, 4));
            return Clamp((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
        }

        if (chunk.SequenceEqual("VP8X"u8))
        {
            // Flags and reserved bytes at 20..23, then canvas width - 1 and height - 1 as 24-bit little-endian values at 24 and 27.
            if (h.Length < 30)
            {
                return null;
            }

            var width = (h[24] | (h[25] << 8) | (h[26] << 16)) + 1;
            var height = (h[27] | (h[28] << 8) | (h[29] << 16)) + 1;
            return Clamp(width, height);
        }

        return null;
    }

    private static ImageSize? Clamp(long width, long height) =>
        width is >= 0 and <= int.MaxValue && height is >= 0 and <= int.MaxValue
            ? new ImageSize((int)width, (int)height)
            : null;
}
