using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// Documents page read contract used by the web Docs, Space page and Boards &amp;
/// Docs surfaces, asserted on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json, regenerated and diffed by CI):
/// every page read declares its DTO instead of <c>System.Void</c>, and the
/// single, list, tree and search reads expose the aggregate <c>version</c>
/// that <c>Documents.Pages.MoveToSpace</c> requires as <c>expectedVersion</c>.
/// </summary>
public class DocumentsOpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    [Theory]
    [InlineData("Documents.Pages.GetPage", "200", "PageDto")]
    [InlineData("Documents.Pages.ListWorkspacePages", "200", "PageDto[]")]
    [InlineData("Documents.Pages.GetPageTree", "200", "PageTreeItemDto[]")]
    [InlineData("Documents.Pages.SearchPages", "200", "PageDto[]")]
    public void PageRead_DeclaresItsResponseType(string operationId, string status, string expected)
    {
        var responses = Operation(operationId).GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().Contain(status, operationId);

        var schema = responses.GetProperty(status).GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Describe(schema).Should().Be(expected, operationId);
    }

    [Theory]
    [InlineData("PageDto", "version")]
    [InlineData("PageDto", "spaceId")]
    [InlineData("PageTreeItemDto", "version")]
    [InlineData("PageTreeItemDto", "spaceId")]
    public void PageReadDto_ExposesField(string dto, string field)
    {
        var schema = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty($"Notrelix.Application.Features.Documents.DTOs.{dto}");
        schema.GetProperty("properties").TryGetProperty(field, out var property).Should().BeTrue($"{dto} must expose {field}");
        if (field == "version")
            property.GetProperty("format").GetString().Should().Be("int64", "the aggregate version is a long, as on BoardDto and WorkspaceDto");
    }

    [Fact]
    public void MoveToSpace_RequiresTheExpectedVersion_InItsBody()
    {
        var body = Operation("Documents.Pages.MoveToSpace").GetProperty("requestBody")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        var request = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty(body.GetProperty("$ref").GetString()!.Split('/').Last());
        request.GetProperty("properties").TryGetProperty("expectedVersion", out _).Should().BeTrue();
    }

    private static string Describe(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            return reference.GetString()!.Split('/').Last().Split('.').Last();
        if (schema.TryGetProperty("type", out var type) && type.GetString() == "array")
            return Describe(schema.GetProperty("items")) + "[]";
        if (schema.TryGetProperty("format", out var format))
            return format.GetString()!;
        return schema.GetProperty("type").GetString()!;
    }

    private static JsonElement Operation(string operationId)
    {
        foreach (var path in Document.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
                if (method.Value.TryGetProperty("operationId", out var id) && id.GetString() == operationId)
                    return method.Value;
        throw new InvalidOperationException($"Operation {operationId} is not in the contract.");
    }

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found (NRX-016)");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}
