using System.Text;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Application.Tests.Features.Collaboration.Attachments;

/// <summary>
/// UC-COL-B06 and UC-COL-B08: the stored content type comes from the leading bytes, never from the client, and only sniffed
/// raster images may be shown inline; every other type, SVG included, is served as a download.
/// </summary>
public sealed class AttachmentContentClassifierTests
{
    private static byte[] Ftyp(string brand) => [0, 0, 0, 0x18, .. "ftyp"u8, .. Encoding.ASCII.GetBytes(brand), 0, 0, 0, 0];

    public static TheoryData<string, byte[], string, string, AttachmentType, bool> Cases => new()
    {
        { "png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1], "a.png", "image/png", AttachmentType.Image, true },
        { "jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10], "a.jpg", "image/jpeg", AttachmentType.Image, true },
        { "gif", Encoding.ASCII.GetBytes("GIF89a\x01\x00"), "a.gif", "image/gif", AttachmentType.Image, true },
        { "webp", Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 "), "a.webp", "image/webp", AttachmentType.Image, true },
        { "pdf", Encoding.ASCII.GetBytes("%PDF-1.7\n"), "a.pdf", "application/pdf", AttachmentType.PDF, false },
        { "docx", [0x50, 0x4B, 0x03, 0x04, 0, 0], "a.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", AttachmentType.Document, false },
        { "zip", [0x50, 0x4B, 0x03, 0x04, 0, 0], "a.zip", "application/zip", AttachmentType.Archive, false },
        { "text", Encoding.UTF8.GetBytes("plain <script>alert(1)</script>"), "a.txt", "text/plain", AttachmentType.Document, false },
        { "heif", Ftyp("heic"), "a.heic", "image/heif", AttachmentType.Image, false },
        { "svg", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"), "a.svg", "text/plain", AttachmentType.Document, false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Classify_UsesTheBytes_AndInlineIsOnlyForTheFourRasterTypes(
        string _, byte[] head, string fileName, string contentType, AttachmentType type, bool inline)
    {
        var classified = AttachmentContentClassifier.Classify(head, fileName);

        classified.ContentType.Should().Be(contentType);
        classified.Type.Should().Be(type);
        AttachmentContentClassifier.IsInlineSafe(classified.ContentType).Should().Be(inline);
    }

    [Fact]
    public void Classify_IgnoresTheExtension_WhenTheBytesSayOtherwise()
    {
        var pngNamedPdf = AttachmentContentClassifier.Classify([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "evil.pdf");

        pngNamedPdf.ContentType.Should().Be("image/png");
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("image/heif")]
    [InlineData("image/avif")]
    [InlineData("application/pdf")]
    [InlineData("text/html")]
    [InlineData("")]
    public void IsInlineSafe_IsFalse_ForEveryOtherType(string contentType) =>
        AttachmentContentClassifier.IsInlineSafe(contentType).Should().BeFalse();
}
