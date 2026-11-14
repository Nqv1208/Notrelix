using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The channel link on the checked-in producer contract (UC-CHT-H10): PUT and DELETE on the channel, and the reverse lookup.</summary>
public class ChatChannelLinkContractOpenApiTests
{
    private const string LinkRoute = "/api/v1/workspaces/{workspaceId}/chat/channels/{channelId}/link";
    private const string LinkedRoute = "/api/v1/workspaces/{workspaceId}/chat/links/linked-channels";
    private static readonly JsonElement Document = Load();

    [Fact]
    public void TheLink_IsAPutAndTheUnlinkADelete_UnderTheirNames_AndDocumentTheirProblems()
    {
        var item = Document.GetProperty("paths").GetProperty(LinkRoute);

        item.GetProperty("put").GetProperty("operationId").GetString().Should().Be("Chat.Channels.Link");
        item.GetProperty("delete").GetProperty("operationId").GetString().Should().Be("Chat.Channels.Unlink");
        foreach (var verb in new[] { "put", "delete" })
            foreach (var status in new[] { "400", "403", "404", "412" })
                item.GetProperty(verb).GetProperty("responses").TryGetProperty(status, out _).Should().BeTrue($"{verb} answers {status}");
    }

    [Fact]
    public void TheReverseLookup_IsAGetWithKindAndId()
    {
        var operation = Document.GetProperty("paths").GetProperty(LinkedRoute).GetProperty("get");

        operation.GetProperty("operationId").GetString().Should().Be("Chat.Channels.ListLinked");
        var names = operation.GetProperty("parameters").EnumerateArray()
            .Where(p => p.GetProperty("in").GetString() == "query").Select(p => p.GetProperty("name").GetString()).ToList();
        names.Should().BeEquivalentTo(["kind", "id"]);
    }

    [Fact]
    public void TheLinkRequest_NamesTheKindTheResourceAndTheVersion()
    {
        var request = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("Notrelix.API.Contracts.Chat.Requests.LinkChannelRequest").GetProperty("properties");

        var names = request.EnumerateObject().Select(p => p.Name).ToList();
        names.Should().BeEquivalentTo(["kind", "resourceId", "expectedVersion"]);
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
