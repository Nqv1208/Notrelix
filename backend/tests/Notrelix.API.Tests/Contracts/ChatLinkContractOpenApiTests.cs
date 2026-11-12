using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The link preview on the checked-in producer contract (UC-CHT-G05, CHT-023): one read, the url or the reference as query, the typed preview, the problems.</summary>
public class ChatLinkContractOpenApiTests
{
    private const string Route = "/api/v1/workspaces/{workspaceId}/chat/links/resolve";
    private const string PreviewSchema = "Notrelix.Application.Features.Chat.Links.DTOs.LinkPreviewDto";
    private static readonly JsonElement Document = Load();

    [Fact]
    public void Resolve_IsAReadOnlyGetWithTheUrlOrTheReferenceAsQuery()
    {
        var path = Document.GetProperty("paths").GetProperty(Route);

        path.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["get"], "resolving never writes");
        var get = path.GetProperty("get");
        get.GetProperty("operationId").GetString().Should().Be("Chat.Links.Resolve");
        get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "url", "kind", "resourceId"]);
    }

    [Fact]
    public void Resolve_ReturnsTheTypedPreview_AndDocumentsTheProblems()
    {
        var responses = Document.GetProperty("paths").GetProperty(Route).GetProperty("get").GetProperty("responses");

        responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()
            .Should().EndWith(PreviewSchema);
        foreach (var status in new[] { "400", "403" })
            responses.TryGetProperty(status, out _).Should().BeTrue($"the preview answers {status}");
    }

    [Fact]
    public void ThePreview_CarriesTheKindTheIdAndATitleOnlyForAReaderWhoCanView()
    {
        Document.GetProperty("components").GetProperty("schemas").GetProperty(PreviewSchema).GetProperty("properties")
            .EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["kind", "resourceId", "accessible", "title"]);
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
