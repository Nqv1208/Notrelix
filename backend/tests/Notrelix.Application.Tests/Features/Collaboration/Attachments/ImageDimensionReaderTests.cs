using System.Text;
using Notrelix.Application.Features.Collaboration.Attachments.Services;

namespace Notrelix.Application.Tests.Features.Collaboration.Attachments;

/// <summary>The header reader of image uploads (UC-CHT-I01): the pixel size comes from the stored bytes, only for the four raster types, and anything unreadable is null.</summary>
public class ImageDimensionReaderTests
{
    private const string Png = "image/png";
    private const string Jpeg = "image/jpeg";
    private const string Gif = "image/gif";
    private const string WebP = "image/webp";

    [Fact]
    public void A_png_reads_the_width_and_height_of_its_ihdr_chunk()
    {
        var size = ImageDimensionReader.TryRead(Png_(64, 32), Png);

        size.Should().Be(new ImageSize(64, 32));
    }

    [Fact]
    public void A_jpeg_reads_the_frame_header_when_it_is_not_the_first_segment()
    {
        // SOI, APP0 (JFIF), APP1 (EXIF-like), DQT, DHT (0xC4 is not a frame marker), then SOF0 with height 300 and width 600.
        byte[] jpeg =
        [
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00,
            0xFF, 0xE1, 0x00, 0x08, 0x45, 0x78, 0x69, 0x66, 0x00, 0x00,
            0xFF, 0xDB, 0x00, 0x04, 0x00, 0x00,
            0xFF, 0xC4, 0x00, 0x04, 0x00, 0x00,
            0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0x2C, 0x02, 0x58, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01,
        ];

        ImageDimensionReader.TryRead(jpeg, Jpeg).Should().Be(new ImageSize(600, 300));
    }

    [Fact]
    public void A_progressive_jpeg_frame_marker_is_read_too()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xC2, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01];

        ImageDimensionReader.TryRead(jpeg, Jpeg).Should().Be(new ImageSize(32, 16));
    }

    [Fact]
    public void A_jpeg_whose_scan_starts_before_any_frame_header_has_no_size()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00];

        ImageDimensionReader.TryRead(jpeg, Jpeg).Should().BeNull();
    }

    [Fact]
    public void A_gif_reads_the_little_endian_logical_screen_size_of_either_version()
    {
        ImageDimensionReader.TryRead(Gif_("GIF89a", 10, 20), Gif).Should().Be(new ImageSize(10, 20));
        ImageDimensionReader.TryRead(Gif_("GIF87a", 300, 2), Gif).Should().Be(new ImageSize(300, 2));
    }

    [Fact]
    public void A_lossy_webp_reads_the_vp8_frame_size_without_its_scale_bits()
    {
        // Width raw 0x4140: the top two bits are the horizontal scale, the size is 0x140 = 320. Height 0x00F0 = 240.
        byte[] header = Riff("VP8 ", [0x30, 0x01, 0x00, 0x9D, 0x01, 0x2A, 0x40, 0x41, 0xF0, 0x00]);

        ImageDimensionReader.TryRead(header, WebP).Should().Be(new ImageSize(320, 240));
    }

    [Fact]
    public void A_lossless_webp_reads_the_packed_width_and_height_plus_one()
    {
        // Width - 1 = 99 in the low 14 bits, height - 1 = 49 in the next 14, little-endian bit order: a 100 x 50 image.
        uint bits = 99u | (49u << 14);
        byte[] payload = [0x2F, (byte)bits, (byte)(bits >> 8), (byte)(bits >> 16), (byte)(bits >> 24)];

        ImageDimensionReader.TryRead(Riff("VP8L", payload), WebP).Should().Be(new ImageSize(100, 50));
    }

    [Fact]
    public void An_extended_webp_reads_the_24_bit_canvas_size_plus_one()
    {
        // Canvas width - 1 = 999 (E7 03 00) and height - 1 = 499 (F3 01 00), after the flags and three reserved bytes.
        byte[] payload = [0x10, 0x00, 0x00, 0x00, 0xE7, 0x03, 0x00, 0xF3, 0x01, 0x00];

        ImageDimensionReader.TryRead(Riff("VP8X", payload), WebP).Should().Be(new ImageSize(1000, 500));
    }

    [Fact]
    public void A_webp_with_a_bad_vp8_start_code_has_no_size()
    {
        byte[] header = Riff("VP8 ", [0x30, 0x01, 0x00, 0x00, 0x00, 0x00, 0x40, 0x01, 0xF0, 0x00]);

        ImageDimensionReader.TryRead(header, WebP).Should().BeNull();
    }

    [Theory]
    [InlineData(Png)]
    [InlineData(Jpeg)]
    [InlineData(Gif)]
    [InlineData(WebP)]
    public void A_truncated_header_has_no_size(string contentType)
    {
        byte[] header = contentType switch
        {
            Png => Png_(64, 32)[..20],
            Jpeg => [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01],
            Gif => Gif_("GIF89a", 10, 20)[..9],
            _ => Riff("VP8L", [0x2F, 0x01, 0x02, 0x03, 0x04])[..24],
        };

        ImageDimensionReader.TryRead(header, contentType).Should().BeNull();
    }

    [Theory]
    [InlineData(Png)]
    [InlineData(Jpeg)]
    [InlineData(Gif)]
    [InlineData(WebP)]
    public void Garbage_has_no_size(string contentType)
    {
        var garbage = Encoding.ASCII.GetBytes("this is not an image, only plain text that happens to be long enough to read");

        ImageDimensionReader.TryRead(garbage, contentType).Should().BeNull();
        ImageDimensionReader.TryRead([], contentType).Should().BeNull();
    }

    [Fact]
    public void A_non_raster_or_unknown_type_has_no_size_even_with_a_png_header()
    {
        var header = Png_(64, 32);

        ImageDimensionReader.TryRead(header, "image/heif").Should().BeNull();
        ImageDimensionReader.TryRead(header, "application/pdf").Should().BeNull();
        ImageDimensionReader.TryRead(header, "text/plain").Should().BeNull();
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(64, 0)]
    [InlineData(20001, 32)]
    [InlineData(64, 20001)]
    public void A_size_outside_the_sane_range_is_refused(int width, int height)
    {
        ImageDimensionReader.TryRead(Png_((uint)width, (uint)height), Png).Should().BeNull();
    }

    [Fact]
    public void The_largest_sane_size_is_accepted()
    {
        ImageDimensionReader.TryRead(Png_(20000, 1), Png).Should().Be(new ImageSize(20000, 1));
    }

    private static byte[] Png_(uint width, uint height)
    {
        var header = new List<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]);
        header.AddRange(Encoding.ASCII.GetBytes("IHDR"));
        header.AddRange(BigEndian(width));
        header.AddRange(BigEndian(height));
        return header.ToArray();
    }

    private static byte[] BigEndian(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] Gif_(string version, ushort width, ushort height)
    {
        var header = new byte[13];
        Encoding.ASCII.GetBytes(version).CopyTo(header, 0);
        BitConverter.GetBytes(width).CopyTo(header, 6);
        BitConverter.GetBytes(height).CopyTo(header, 8);
        return header;
    }

    private static byte[] Riff(string chunk, byte[] payload)
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("RIFF"));
        bytes.AddRange(BitConverter.GetBytes((uint)(4 + 8 + payload.Length)));
        bytes.AddRange(Encoding.ASCII.GetBytes("WEBP"));
        bytes.AddRange(Encoding.ASCII.GetBytes(chunk));
        bytes.AddRange(BitConverter.GetBytes((uint)payload.Length));
        bytes.AddRange(payload);
        return bytes.ToArray();
    }
}
