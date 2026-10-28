using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The Item history read on the checked-in producer contract (backend/contracts/openapi/notrelix.v1.json): the
/// route answers a typed page of history lines, each with the stable type, the actor and a small language-neutral
/// change, never a free sentence only.
/// </summary>
public class ItemActivityContractOpenApiTests
{
    private const string Route = "/api/v1/board-items/{boardItemId}/activity";
    private const string PageSchema = "Notrelix.Application.Features.Collaboration.Activity.DTOs.ResourceActivityPageDto";
    private const string LineSchema = "Notrelix.Application.Features.Collaboration.Activity.DTOs.ActivityLogDto";
    private const string ChangeSchema = "Notrelix.Application.Features.Collaboration.Activity.DTOs.ActivityChangeDto";
    private static readonly JsonElement Document = Load();
    private static JsonElement Schemas => Document.GetProperty("components").GetProperty("schemas");

    [Fact]
    public void TheRoute_AnswersATypedPage_AndKeepsItsPagingParameters()
    {
        var get = Document.GetProperty("paths").GetProperty(Route).GetProperty("get");

        get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString())
            .Should().Contain(["boardItemId", "page", "pageSize"]);
        get.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith(PageSchema);
    }

    [Fact]
    public void ThePage_CarriesTheLinesAndPaging()
    {
        Schemas.GetProperty(PageSchema).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["data", "total", "page", "pageSize"]);
    }

    [Fact]
    public void ALine_KeepsItsFields_AndCarriesTheTypeAndTheChange()
    {
        var properties = Schemas.GetProperty(LineSchema).GetProperty("properties");

        properties.EnumerateObject().Select(p => p.Name).Should().Contain(
            ["id", "actorId", "actorName", "action", "resourceKind", "resourceId", "createdAt", "actorAvatarUrl", "type", "kind", "change"]);
        Schemas.GetProperty(ChangeSchema).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["fieldName", "from", "to", "personId", "personName", "itemKey"]);
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        // Resolved from this source file, so the lookup does not depend on where build output goes.
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}
