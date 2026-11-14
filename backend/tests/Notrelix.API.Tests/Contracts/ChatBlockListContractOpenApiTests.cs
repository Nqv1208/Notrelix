using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The caller's own block list on the checked-in producer contract (UC-CHT-E06).</summary>
public class ChatBlockListContractOpenApiTests
{
    private static readonly JsonElement Document = Load();

    [Fact]
    public void TheBlockList_IsAGetOnTheCollection_BesideThePut_AndTakesNoPersonId()
    {
        var item = Document.GetProperty("paths").GetProperty("/api/v1/workspaces/{workspaceId}/chat/me/blocks");

        var get = item.GetProperty("get");
        get.GetProperty("operationId").GetString().Should().Be("Chat.Blocks.List");
        item.GetProperty("put").GetProperty("operationId").GetString().Should().Be("Chat.Blocks.Set");
        get.GetProperty("parameters").EnumerateArray()
            .Select(p => p.GetProperty("name").GetString()).Should().BeEquivalentTo(["workspaceId"], "the list is the caller's own and names no other person");
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
