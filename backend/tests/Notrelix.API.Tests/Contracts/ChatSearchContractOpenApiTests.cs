using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The message search on the checked-in producer contract (UC-CHT-H07).</summary>
public class ChatSearchContractOpenApiTests
{
    private const string Route = "/api/v1/workspaces/{workspaceId}/chat/search";
    private static readonly JsonElement Document = Load();

    [Fact]
    public void TheSearch_IsPublishedAsAGetUnderItsName_AndDocumentsItsProblems()
    {
        Document.GetProperty("paths").TryGetProperty(Route, out var item).Should().BeTrue();
        var operation = item.GetProperty("get");
        operation.GetProperty("operationId").GetString().Should().Be("Chat.Search.Query");
        foreach (var status in new[] { "400", "403", "429" })
            operation.GetProperty("responses").TryGetProperty(status, out _).Should().BeTrue($"search answers {status}");
    }

    [Fact]
    public void TheSearch_TakesTheQueryTheFiltersAndTheCursorAsQueryParameters()
    {
        var parameters = Document.GetProperty("paths").GetProperty(Route).GetProperty("get").GetProperty("parameters")
            .EnumerateArray().Where(p => p.GetProperty("in").GetString() == "query").Select(p => p.GetProperty("name").GetString()).ToList();
        parameters.Should().BeEquivalentTo(["q", "spaceId", "authorId", "from", "to", "hasAttachment", "mentionsMe", "cursor", "limit"]);
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
