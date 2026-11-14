using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The retention rule and the export of a person's chat data on the checked-in producer contract (UC-CHT-H05, H06).</summary>
public class ChatRetentionExportContractOpenApiTests
{
    private const string RetentionRoute = "/api/v1/workspaces/{workspaceId}/chat/retention";
    private const string ExportsRoute = "/api/v1/workspaces/{workspaceId}/chat/exports";
    private const string ExportRoute = "/api/v1/workspaces/{workspaceId}/chat/exports/{exportId}";
    private const string ContentRoute = "/api/v1/workspaces/{workspaceId}/chat/exports/{exportId}/content";
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");

    [Theory]
    [InlineData(RetentionRoute, "get", "Chat.Retention.Get")]
    [InlineData(RetentionRoute, "put", "Chat.Retention.Set")]
    [InlineData(ExportsRoute, "post", "Chat.Exports.Create")]
    [InlineData(ExportRoute, "get", "Chat.Exports.Get")]
    [InlineData(ContentRoute, "get", "Chat.Exports.GetContent")]
    public void Operation_IsPublishedUnderItsName(string path, string verb, string operationId)
    {
        Paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} is part of the contract");
        item.TryGetProperty(verb, out var operation).Should().BeTrue($"{verb} {path}");
        operation.GetProperty("operationId").GetString().Should().Be(operationId);
    }

    [Theory]
    [InlineData(RetentionRoute, "put", new[] { "400", "403", "412" })]
    [InlineData(ExportsRoute, "post", new[] { "400", "403", "404", "429" })]
    [InlineData(ContentRoute, "get", new[] { "403", "404" })]
    public void TheOperations_DocumentTheirProblems(string path, string verb, string[] statuses)
    {
        var responses = Paths.GetProperty(path).GetProperty(verb).GetProperty("responses");
        foreach (var status in statuses)
            responses.TryGetProperty(status, out _).Should().BeTrue($"{verb} {path} answers {status}");
    }

    [Fact]
    public void TheDownloadIsAStreamOfJson_NotAPublicUrl()
    {
        var ok = Paths.GetProperty(ContentRoute).GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content");
        ok.TryGetProperty("application/json", out _).Should().BeTrue();
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
