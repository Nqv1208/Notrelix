using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The page history and page template routes on the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json): list, read, name and restore a version; save a page as a
/// template, list templates and create a page from one. The existing history route is unchanged.
/// </summary>
public class PageHistoryAndTemplatesContractOpenApiTests
{
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");
    private static JsonElement Schemas => Document.GetProperty("components").GetProperty("schemas");

    [Theory]
    [InlineData("/api/v1/pages/{pageId}/history", "get")]
    [InlineData("/api/v1/pages/{pageId}/versions", "get")]
    [InlineData("/api/v1/pages/{pageId}/versions", "post")]
    [InlineData("/api/v1/pages/{pageId}/versions/{versionId}", "get")]
    [InlineData("/api/v1/pages/{pageId}/versions/{versionId}/restore", "post")]
    [InlineData("/api/v1/workspaces/{workspaceId}/page-templates", "get")]
    [InlineData("/api/v1/workspaces/{workspaceId}/page-templates", "post")]
    [InlineData("/api/v1/workspaces/{workspaceId}/page-templates/{templateId}/pages", "post")]
    public void TheRoutes_Exist(string path, string verb)
    {
        Paths.GetProperty(path).TryGetProperty(verb, out _).Should().BeTrue($"{verb} {path}");
    }

    [Fact]
    public void ARestore_AnswersWithTheNewVersionAndTheBlocks_AndCanAnswer412()
    {
        var operation = Paths.GetProperty("/api/v1/pages/{pageId}/versions/{versionId}/restore").GetProperty("post");

        operation.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        operation.GetProperty("responses").TryGetProperty("412", out _).Should().BeTrue("a restore is guarded by the versions the caller saw");
        Schema("RestorePageVersionRequest").GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().Equal("currentBlocks");
        Schema("RestorePageVersionResultDto").GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["version", "blocks"]);
    }

    [Fact]
    public void AVersion_NamesItsKind_AndItsPlaceInThePagesHistory()
    {
        Schema("PageVersionDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["id", "pageId", "versionNumber", "kind", "name", "createdBy", "createdAt"]);
        Schema("PageVersionDetailDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["version", "title", "blocks"]);
        Schema("PageVersionBlockDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["id", "parentId", "type", "content", "properties", "position"], "a captured block carries no concurrency version");
    }

    [Fact]
    public void ATemplate_IsCreationInput_AndAPageFromOneSaysWhatWasLeftOut()
    {
        Schema("PageTemplateDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["id", "name", "description", "title", "icon", "blockCount", "createdBy", "createdAt"]);
        Schema("PageFromTemplateDto").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["pageId", "droppedBlockCount"]);
        Schema("CreatePageFromTemplateRequest").GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(["title", "parentId", "spaceId"]);
    }

    private static JsonElement Schema(string name) => Schemas.EnumerateObject().Single(s => s.Name.EndsWith("." + name, StringComparison.Ordinal)).Value;

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
