using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Conversations and blocks on the checked-in producer contract: routes, verbs, parameters and typed responses (UC-CHT-E01 to E07).</summary>
public class ChatConversationContractOpenApiTests
{
    private const string Base = "/api/v1/workspaces/{workspaceId}/chat/conversations";
    private const string BlocksRoute = "/api/v1/workspaces/{workspaceId}/chat/me/blocks";
    private static readonly JsonElement Document = Load();

    private static JsonElement Operation(string route, string verb) =>
        Document.GetProperty("paths").GetProperty(route).GetProperty(verb);

    private static string Ref(JsonElement response) =>
        response.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;

    [Fact]
    public void TheConversationRoutes_OfferStartListAddLeaveAndVisibility()
    {
        var paths = Document.GetProperty("paths");

        paths.GetProperty(Base).TryGetProperty("post", out _).Should().BeTrue("start is a POST");
        paths.GetProperty(Base).TryGetProperty("get", out _).Should().BeTrue("the list is a GET");
        paths.GetProperty(Base + "/{conversationId}/participants").TryGetProperty("post", out _).Should().BeTrue();
        paths.GetProperty(Base + "/{conversationId}/participants/me").TryGetProperty("delete", out _).Should().BeTrue("leave is a DELETE");
        paths.GetProperty(Base + "/{conversationId}/visibility").TryGetProperty("put", out _).Should().BeTrue("hide and show is an idempotent PUT");
        paths.GetProperty(BlocksRoute).TryGetProperty("put", out _).Should().BeTrue("block and unblock is an idempotent PUT");
    }

    [Fact]
    public void Start_TakesTheIdempotencyKey_AndReturnsTheTypedResult()
    {
        var start = Operation(Base, "post");

        start.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().Contain(["workspaceId", "Idempotency-Key"]);
        Ref(start.GetProperty("responses").GetProperty("201")).Should().EndWith("Chat.Conversations.DTOs.StartConversationResultDto");
        start.GetProperty("responses").TryGetProperty("422", out _).Should().BeTrue("a group over the cap is limit-reached");
    }

    [Fact]
    public void TheList_TakesALimit_AndTheOtherRoutesTakeTheConversationInThePath()
    {
        Operation(Base, "get").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().Contain(["workspaceId", "limit"]);
        foreach (var (route, verb) in new[] { ("/{conversationId}/participants", "post"), ("/{conversationId}/participants/me", "delete"), ("/{conversationId}/visibility", "put") })
        {
            Operation(Base + route, verb).GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
                .Should().Contain(["workspaceId", "conversationId"]);
        }
    }

    [Fact]
    public void TheConversation_IsDocumentedWithItsParticipantsPreviewAndUnreadCount()
    {
        var schemas = Document.GetProperty("components").GetProperty("schemas");
        var conversation = schemas.GetProperty("Notrelix.Application.Features.Chat.Conversations.DTOs.ConversationDto");

        conversation.GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().Contain(["id", "kind", "participants", "unreadCount"]);
        Operation(Base, "get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("items").GetProperty("$ref").GetString().Should().EndWith("Chat.Conversations.DTOs.ConversationDto");
        Ref(Operation(Base + "/{conversationId}/visibility", "put").GetProperty("responses").GetProperty("200")).Should().EndWith("ConversationVisibilityDto");
        Ref(Operation(Base + "/{conversationId}/participants", "post").GetProperty("responses").GetProperty("200")).Should().EndWith("AddConversationParticipantsResultDto");
        Ref(Operation(BlocksRoute, "put").GetProperty("responses").GetProperty("200")).Should().EndWith("Chat.Blocks.DTOs.PersonBlockDto");
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
