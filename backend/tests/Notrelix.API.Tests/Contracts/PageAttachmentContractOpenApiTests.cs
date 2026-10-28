using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The page attachment contract on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json): the five page routes exist next to the
/// unchanged board item routes, the upload is multipart with an idempotency key, and the
/// content download is the only way to read the bytes (no public address).
/// </summary>
public class PageAttachmentContractOpenApiTests
{
    private const string AttachmentDtoSchema = "Notrelix.Application.Features.Collaboration.Attachments.DTOs.AttachmentDto";
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");

    [Theory]
    [InlineData("/api/v1/pages/{pageId}/attachments", "get")]
    [InlineData("/api/v1/pages/{pageId}/attachments", "post")]
    [InlineData("/api/v1/pages/{pageId}/attachments/files", "post")]
    [InlineData("/api/v1/pages/{pageId}/attachments/{attachmentId}/content", "get")]
    [InlineData("/api/v1/pages/{pageId}/attachments/{attachmentId}", "delete")]
    [InlineData("/api/v1/board-items/{boardItemId}/attachments/{attachmentId}/content", "get")]
    public void Operation_IsPublished(string path, string verb)
    {
        Paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} is part of the attachment contract");
        item.TryGetProperty(verb, out _).Should().BeTrue($"{verb} {path}");
    }

    [Fact]
    public void PageUpload_IsMultipart_AndReturnsTheAttachmentDto()
    {
        var upload = Paths.GetProperty("/api/v1/pages/{pageId}/attachments/files").GetProperty("post");

        upload.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _).Should().BeTrue();
        var created = upload.GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json");
        created.GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith(AttachmentDtoSchema);
        upload.GetProperty("responses").TryGetProperty("415", out _).Should().BeTrue();
        upload.GetProperty("responses").TryGetProperty("413", out _).Should().BeTrue();
    }

    [Fact]
    public void AttachmentDto_KeepsItsShape_ForBothTargets()
    {
        var schema = Document.GetProperty("components").GetProperty("schemas").GetProperty(AttachmentDtoSchema).GetProperty("properties");

        schema.EnumerateObject().Select(p => p.Name).Should().Contain(
            ["id", "resourceId", "filename", "url", "sizeBytes", "contentType", "source", "uploadedBy", "uploadedByName", "createdAt", "kind"]);
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        // Resolved from this source file, so the lookup does not depend on where build output goes.
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}
