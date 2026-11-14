using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Channel permission overrides and the moderator removal on the checked-in producer contract (UC-CHT-H01, H03).</summary>
public class ChatPermissionContractOpenApiTests
{
    private const string PermissionsRoute = "/api/v1/workspaces/{workspaceId}/chat/channels/{channelId}/permissions";
    private const string ModerationRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/moderation";
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");

    [Theory]
    [InlineData(PermissionsRoute, "get", "Chat.Permissions.Get")]
    [InlineData(PermissionsRoute, "put", "Chat.Permissions.Set")]
    [InlineData(PermissionsRoute, "delete", "Chat.Permissions.Delete")]
    [InlineData(ModerationRoute, "post", "Chat.Messages.Moderate")]
    public void Operation_IsPublishedUnderItsName(string path, string verb, string operationId)
    {
        Paths.TryGetProperty(path, out var item).Should().BeTrue($"{path} is part of the contract");
        item.TryGetProperty(verb, out var operation).Should().BeTrue($"{verb} {path}");
        operation.GetProperty("operationId").GetString().Should().Be(operationId);
    }

    [Theory]
    [InlineData(PermissionsRoute, "put", new[] { "400", "403", "404", "409", "412", "422" })]
    [InlineData(PermissionsRoute, "delete", new[] { "400", "403", "404", "409", "412" })]
    [InlineData(ModerationRoute, "post", new[] { "400", "403", "404", "409", "412" })]
    public void TheWrites_DocumentTheStaleVersionAndPermissionProblems(string path, string verb, string[] statuses)
    {
        var responses = Paths.GetProperty(path).GetProperty(verb).GetProperty("responses");
        foreach (var status in statuses)
            responses.TryGetProperty(status, out _).Should().BeTrue($"{verb} {path} answers {status}");
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
