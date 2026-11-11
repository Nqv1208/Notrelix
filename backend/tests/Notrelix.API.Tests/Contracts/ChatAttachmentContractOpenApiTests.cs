using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Files on a message on the checked-in producer contract: the routes, the multipart upload with an idempotency key, the typed file list and the message view that carries it (UC-CHT-G08, CHT-012).</summary>
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
            .Should().BeEquivalentTo(["id", "messageId", "fileName", "sizeBytes", "contentType", "uploadedBy", "createdAt", "downloadPath"]);
    }

    [Fact]
    public void TheMessageView_CarriesTheFileList()
    {
        Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("Notrelix.Application.Features.Chat.Messages.DTOs.MessageDto").GetProperty("properties")
            .TryGetProperty("attachments", out var attachments).Should().BeTrue();
        attachments.GetProperty("items").GetProperty("$ref").GetString().Should().EndWith(AttachmentSchema);
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
