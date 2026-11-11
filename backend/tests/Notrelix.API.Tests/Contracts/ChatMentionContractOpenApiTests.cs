using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Mentions on the checked-in producer contract: the picker route, the mention fields of the send, edit and reply bodies (UC-CHT-G01 to G04). Message responses are untyped in the document, as for every Chat message route, so their mention fields are pinned by the application tests and the frontend contract.</summary>
public class ChatMentionContractOpenApiTests
{
    private const string MentionablesRoute = "/api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/mentionables";
    private static readonly JsonElement Document = Load();

    private static JsonElement Schema(string suffix)
    {
        var schemas = Document.GetProperty("components").GetProperty("schemas");
        foreach (var s in schemas.EnumerateObject())
            if (s.Name.EndsWith(suffix, StringComparison.Ordinal))
                return s.Value;
        throw new InvalidOperationException($"No schema ends with {suffix}.");
    }

    private static IEnumerable<string> Props(string suffix) => Schema(suffix).GetProperty("properties").EnumerateObject().Select(p => p.Name);

    [Fact]
    public void TheMentionPicker_IsAGetOnTheSpace_WithTheTypedAnswer()
    {
        var get = Document.GetProperty("paths").GetProperty(MentionablesRoute).GetProperty("get");

        get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "spaceId"]);
        get.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()
            .Should().EndWith("Chat.Mentions.DTOs.MentionablesDto");
    }

    [Fact]
    public void ThePickerAnswer_NamesThePeopleTheBroadcastRightAndTheThreshold()
    {
        Props("Chat.Mentions.DTOs.MentionablesDto").Should().BeEquivalentTo(["people", "canBroadcast", "broadcastConfirmAbove", "memberCount"]);
        Props("Chat.Mentions.DTOs.MentionableDto").Should().BeEquivalentTo(["userId", "inSpace"]);
    }

    [Fact]
    public void ASendEditAndReplyBody_CarryTheStructuredMentionsAndTheBroadcastConfirmation()
    {
        foreach (var body in new[] { "SendMessageRequest", "EditMessageRequest", "ReplyInThreadRequest" })
            Props(body).Should().Contain(["mentions", "confirmBroadcast"], body);

        Props("MentionRequest").Should().BeEquivalentTo(["kind", "start", "length", "userId", "spaceId"]);
    }

    [Fact]
    public void TheSendDescribesTheMentionChecksAndTheBroadcastConfirmation()
    {
        var send = Document.GetProperty("paths").GetProperty("/api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages").GetProperty("post");

        var description = send.GetProperty("description").GetString()!;
        description.Should().Contain("mentionNotices").And.Contain("broadcast-confirmation-required").And.Contain("mention-invalid");
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
