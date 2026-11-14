using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Files on a message on the checked-in producer contract: the routes, the multipart upload with an idempotency key, and the typed file (UC-CHT-G08, CHT-012). The message views are not typed in the contract (the Chat message routes declare problems only), so the file list in a message view is covered by the handler tests.</summary>
public class ChatAttachmentContractOpenApiTests
{
    private const string ListRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments";
    private const string ContentRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments/{attachmentId}/content";
    private const string ItemRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments/{attachmentId}";
    private const string AttachmentSchema = "Notrelix.Application.Features.Chat.Attachments.DTOs.MessageAttachmentDto";
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");

    [Theory]
    [InlineData(ListRoute, "get")]
    [InlineData(ListRoute, "post")]
    [InlineData(ContentRoute, "get")]
    [InlineData(ItemRoute, "delete")]
    public void Operation_IsPublished(string path, string verb)
    {
        Paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} is part of the contract");
        item.TryGetProperty(verb, out _).Should().BeTrue($"{verb} {path}");
    }

    [Fact]
    public void TheUpload_IsMultipart_WithAnIdempotencyKey_AndReturnsTheTypedAttachment()
    {
        var upload = Paths.GetProperty(ListRoute).GetProperty("post");

        upload.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _).Should().BeTrue();
        upload.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString()).Should().Contain("Idempotency-Key");
        upload.GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith(AttachmentSchema);
        foreach (var status in new[] { "400", "403", "404", "409", "413", "415" })
            upload.GetProperty("responses").TryGetProperty(status, out _).Should().BeTrue($"the shared file policy answers {status}");
    }

    [Fact]
    public void TheDownload_IsTheOnlyWayToReadTheBytes_AndHasNoPublicAddress()
    {
        var content = Paths.GetProperty(ContentRoute).GetProperty("get");

        content.GetProperty("responses").GetProperty("200").GetProperty("content").TryGetProperty("application/octet-stream", out _).Should().BeTrue();
        content.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "messageId", "attachmentId"]);
    }

    [Fact]
    public void TheAttachment_IsDocumentedWithMetadataAndADownloadPath()
    {
        Document.GetProperty("components").GetProperty("schemas").GetProperty(AttachmentSchema).GetProperty("properties")
            .EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["id", "messageId", "fileName", "sizeBytes", "contentType", "uploadedBy", "createdAt", "downloadPath", "width", "height", "durationMs", "waveform"]);
    }

    [Fact]
    public void TheImageSize_IsOptionalNullableWholeNumbers()
    {
        var properties = Document.GetProperty("components").GetProperty("schemas").GetProperty(AttachmentSchema).GetProperty("properties");

        foreach (var side in new[] { "width", "height" })
        {
            var property = properties.GetProperty(side);
            property.GetProperty("type").GetString().Should().Be("integer", $"{side} is a whole number of pixels");
            property.GetProperty("nullable").GetBoolean().Should().BeTrue($"{side} is null for a file that is not an image");
        }

        var required = Document.GetProperty("components").GetProperty("schemas").GetProperty(AttachmentSchema)
            .TryGetProperty("required", out var list) ? list.EnumerateArray().Select(r => r.GetString()).ToList() : [];
        required.Should().NotContain(["width", "height"]);
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}
