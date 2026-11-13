using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>The sidebar category names on the checked-in producer contract (UC-CHT-A02).</summary>
public class ChatChannelGroupListContractOpenApiTests
{
    private static readonly JsonElement Document = Load();

    [Fact]
    public void TheCategoryList_IsAGetOnTheCollection_BesideThePost()
    {
        var item = Document.GetProperty("paths").GetProperty("/api/v1/workspaces/{workspaceId}/chat/channel-groups");

        item.GetProperty("get").GetProperty("operationId").GetString().Should().Be("Chat.ChannelGroups.List");
        item.GetProperty("post").GetProperty("operationId").GetString().Should().Be("Chat.ChannelGroups.Create");
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
