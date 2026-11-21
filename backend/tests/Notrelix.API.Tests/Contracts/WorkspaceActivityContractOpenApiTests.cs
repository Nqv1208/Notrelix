using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The workspace activity read on the checked-in producer contract (backend/contracts/openapi/notrelix.v1.json): a cursor page
/// of feed lines, each with the stable type, the actor and the subject, never a free sentence only.
/// </summary>
public class WorkspaceActivityContractOpenApiTests
{
    private const string Route = "/api/v1/workspaces/{workspaceId}/activity";
    private const string PageSchema = "Notrelix.Application.Common.Models.CursorPageOfWorkspaceActivityEntryDto";
    private const string LineSchema = "Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity.WorkspaceActivityEntryDto";
    private static readonly JsonElement Document = Load();
    private static JsonElement Schemas => Document.GetProperty("components").GetProperty("schemas");

    [Fact]
    public void TheRoute_AnswersACursorPage_WithACursorAndALimit_AndNoPageNumber()
    {
        var get = Document.GetProperty("paths").GetProperty(Route).GetProperty("get");

        var names = get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        names.Should().Contain(["workspaceId", "cursor", "limit"]);
        names.Should().NotContain(["page", "pageSize"]);
        get.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith(PageSchema);
    }

    [Fact]
    public void ALine_CarriesTheTypeTheActorAndTheSubject()
    {
        Schemas.GetProperty(PageSchema).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().Contain(["items", "nextCursor"]);
        Schemas.GetProperty(LineSchema).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo(
                ["id", "type", "actorUserId", "actorName", "actorAvatarUrl", "title", "subjectType", "subjectId", "subjectName", "occurredAt"]);
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
