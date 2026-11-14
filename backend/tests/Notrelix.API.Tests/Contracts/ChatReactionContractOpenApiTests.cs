using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Reactions on the checked-in producer contract: the reaction routes, their verbs, parameters and typed responses (UC-CHT-G06, G07).</summary>
public class ChatReactionContractOpenApiTests
{
    private const string ReactionRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/reactions/{kind}";
    private const string ReactorsRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/reactions";
    private static readonly JsonElement Document = Load();

    [Fact]
    public void TheReactionRoute_OffersAddAndRemove_OnTheMessageAndKind()
    {
        var path = Document.GetProperty("paths").GetProperty(ReactionRoute);

        path.TryGetProperty("put", out _).Should().BeTrue("react is a PUT, idempotent by nature");
        path.TryGetProperty("delete", out _).Should().BeTrue("remove is a DELETE");
    }

    [Fact]
    public void TheReactionRoute_TakesTheWorkspaceTheMessageAndTheKindInThePath()
    {
        var path = Document.GetProperty("paths").GetProperty(ReactionRoute);

        path.GetProperty("put").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "messageId", "kind"]);
        path.GetProperty("delete").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "messageId", "kind"]);
    }

    [Fact]
    public void TheReactorsRoute_IsAGetOnTheMessage_WithTheKindFilterAndThePaging()
    {
        var path = Document.GetProperty("paths").GetProperty(ReactorsRoute);

        path.TryGetProperty("get", out _).Should().BeTrue();
        path.GetProperty("get").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().Contain(["workspaceId", "messageId", "kind", "cursor", "limit"]);
    }

    [Fact]
    public void TheReactionWrites_ReturnTheTypedReactionSummary()
    {
        var put = Document.GetProperty("paths").GetProperty(ReactionRoute).GetProperty("put");

        // The new reaction is 201 with the summary. The repeat (200, no change) carries the same body and is described on the operation.
        var responses = put.GetProperty("responses");
        responses.GetProperty("201").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()
            .Should().EndWith("Chat.Reactions.DTOs.MessageReactionsDto");
    }

    [Fact]
    public void TheReactorsRead_ReturnsTheTypedReactorPage()
    {
        var get = Document.GetProperty("paths").GetProperty(ReactorsRoute).GetProperty("get");

        get.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()
            .Should().EndWith("Chat.Reactions.DTOs.ReactorPageDto");
    }

    [Fact]
    public void TheReactionSummary_IsDocumentedWithEmojiCountAndReactedByMe()
    {
        var summary = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("Notrelix.Application.Features.Chat.Reactions.DTOs.ReactionSummaryDto");

        summary.GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["emoji", "count", "reactedByMe"]);
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
