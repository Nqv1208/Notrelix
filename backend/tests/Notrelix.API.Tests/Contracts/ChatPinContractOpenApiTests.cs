using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Pins on the checked-in producer contract: the pin routes, their verbs and their parameters.</summary>
public class ChatPinContractOpenApiTests
{
    private const string PinRoute = "/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/pin";
    private const string PinsRoute = "/api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/pins";
    private static readonly JsonElement Document = Load();

    [Fact]
    public void ThePinRoute_OffersPinAndUnpin_OnTheMessage()
    {
        var path = Document.GetProperty("paths").GetProperty(PinRoute);

        path.TryGetProperty("put", out _).Should().BeTrue("pin is a PUT, idempotent by nature");
        path.TryGetProperty("delete", out _).Should().BeTrue("unpin is a DELETE");
    }

    [Fact]
    public void ThePinRoute_TakesTheWorkspaceAndTheMessageInThePath()
    {
        var put = Document.GetProperty("paths").GetProperty(PinRoute).GetProperty("put");

        put.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["workspaceId", "messageId"]);
    }

    [Fact]
    public void ThePinsRoute_IsAGetOnTheSpace()
    {
        var path = Document.GetProperty("paths").GetProperty(PinsRoute);

        path.TryGetProperty("get", out _).Should().BeTrue();
        path.GetProperty("get").GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().Contain(["workspaceId", "spaceId", "cursor", "limit"]);
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
