using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// API-TRANSPORT-001 / API-PLACEMENT-001 / API-UPDATES-001 / API-VERSION-001
/// (WM-V3-API-001). Asserts the V3 shapes of the checked-in producer contract
/// (backend/contracts/openapi/notrelix.v1.json, regenerated and diffed by CI):
/// placement is neighbor-relative with no raw key, every P3 versioned mutation
/// carries expectedVersion, PATCH bodies hold exactly their accepted fields, no P3
/// body is an Application command, and every mutable read DTO exposes version.
/// </summary>
public class WorkP3OpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    private static readonly string[] PlacementOperations =
    [
        "WorkManagement.BoardItems.Create", "WorkManagement.BoardItems.Move",
        "WorkManagement.BoardGroups.Create", "WorkManagement.BoardGroups.Move",
        "WorkManagement.BoardFields.Create", "WorkManagement.BoardFields.Move",
        "WorkManagement.BoardFields.AddFieldOption", "WorkManagement.BoardFields.MoveFieldOption",
        "WorkManagement.Checklists.Create", "WorkManagement.Checklists.CreateItemByChecklist",
        "WorkManagement.Checklists.Move", "WorkManagement.Checklists.MoveItem",
    ];

    private static readonly string[] VersionedBodyOperations =
    [
        "WorkManagement.Boards.Rename", "WorkManagement.BoardItems.Update", "WorkManagement.BoardItems.Move",
        "WorkManagement.BoardItems.UpdateFieldValue", "WorkManagement.BoardItems.UpdateFieldValues",
        "WorkManagement.BoardFields.Update", "WorkManagement.BoardFields.Move", "WorkManagement.BoardFields.AddFieldOption",
        "WorkManagement.BoardFields.UpdateFieldOption", "WorkManagement.BoardFields.MoveFieldOption",
        "WorkManagement.BoardGroups.Update", "WorkManagement.BoardGroups.Move",
        "WorkManagement.Checklists.Update", "WorkManagement.Checklists.Move",
        "WorkManagement.Checklists.CreateItemByChecklist", "WorkManagement.Checklists.MoveItem", "WorkManagement.Checklists.UpdateItem",
    ];

    private static readonly string[] VersionedQueryOperations =
    [
        "WorkManagement.BoardItems.ClearFieldValue", "WorkManagement.BoardFields.RemoveFieldOption",
        "WorkManagement.BoardGroups.Delete", "WorkManagement.Checklists.DeleteItem", "WorkManagement.Checklists.ToggleItem",
    ];

    private static readonly string[] VersionedReadDtos =
    [
        "BoardDto", "FullBoardDto", "BoardSchemaDto", "BoardGroupDto", "BoardGroupSchemaDto", "BoardFieldDto",
        "BoardFieldSchemaDto", "BoardItemDto", "BoardItemSummaryDto", "BoardItemSlimDto", "ChecklistDto",
    ];

    [Fact]
    public void Placement_IsNeighborRelative_WithNoRawKey()
    {
        foreach (var operationId in PlacementOperations)
        {
            var properties = BodyProperties(operationId);
            properties.Should().Contain(["previousSiblingId", "nextSiblingId"], operationId);
            properties.Should().NotContain(["position", "newPosition", "items", "orderedOptionIds"], $"{operationId} must not accept a raw ordering key");
        }
    }

    [Fact]
    public void EveryP3VersionedMutation_CarriesExpectedVersion()
    {
        foreach (var operationId in VersionedBodyOperations)
            BodyProperties(operationId).Should().Contain("expectedVersion", operationId);
        foreach (var operationId in VersionedQueryOperations)
            QueryParameters(operationId).Should().Contain("expectedVersion", operationId);

        (VersionedBodyOperations.Length + VersionedQueryOperations.Length).Should().Be(22,
            "one transport per entry of the P3 versioned-request manifest");
    }

    [Theory]
    [InlineData("WorkManagement.BoardItems.Update", "expectedVersion,title,dueDate,startDate,descriptionMd,clearDueDate,clearStartDate")]
    [InlineData("WorkManagement.BoardFields.Update", "name,settingsJson,expectedVersion")]
    [InlineData("WorkManagement.Checklists.UpdateItem", "expectedVersion,title,isChecked")]
    [InlineData("WorkManagement.Checklists.Update", "expectedVersion,title")]
    [InlineData("WorkManagement.Boards.Rename", "expectedVersion,title,description,background,visibility,itemKeyPrefix")]
    public void PatchBodies_HoldExactlyTheirAcceptedFields(string operationId, string fields)
    {
        BodyProperties(operationId).Should().BeEquivalentTo(fields.Split(','));
    }

    [Fact]
    public void NoP3RequestBody_IsAnApplicationCommand()
    {
        var commandBodies = Operations()
            .Where(op => op.Body is not null && op.Body.EndsWith("Command", StringComparison.Ordinal))
            .Select(op => $"{op.OperationId} -> {op.Body}");

        commandBodies.Should().BeEmpty("a command body exposes route ids that the endpoint would silently overwrite");
    }

    [Fact]
    public void EveryMutableReadDto_ExposesVersion()
    {
        var schemas = Document.GetProperty("components").GetProperty("schemas");
        foreach (var dto in VersionedReadDtos)
        {
            var schema = schemas.EnumerateObject().Single(s => s.Name.EndsWith("." + dto, StringComparison.Ordinal)).Value;
            schema.GetProperty("properties").TryGetProperty("version", out _).Should().BeTrue($"{dto} must expose the aggregate version (WM-V3-CONC-002)");
        }
    }

    private static IEnumerable<(string OperationId, string? Body, JsonElement Operation)> Operations()
    {
        foreach (var path in Document.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
            {
                var id = method.Value.TryGetProperty("operationId", out var op) ? op.GetString()! : "";
                if (!IsP3(id)) continue;
                string? body = null;
                if (method.Value.TryGetProperty("requestBody", out var rb)
                    && rb.GetProperty("content").TryGetProperty("application/json", out var json)
                    && json.GetProperty("schema").TryGetProperty("$ref", out var reference))
                {
                    body = reference.GetString()!.Split('/').Last().Split('.').Last();
                }

                yield return (id, body, method.Value);
            }
    }

    private static bool IsP3(string operationId) =>
        operationId.Split('.') is ["WorkManagement", var module, _] && module is "Boards" or "BoardItems" or "BoardFields" or "BoardGroups" or "Checklists" or "Labels";

    private static IReadOnlyList<string> BodyProperties(string operationId)
    {
        var operation = Operations().Single(op => op.OperationId == operationId);
        operation.Body.Should().NotBeNull($"{operationId} has a JSON body");
        var schema = Document.GetProperty("components").GetProperty("schemas").EnumerateObject()
            .Single(s => s.Name.Split('.').Last() == operation.Body).Value;
        return schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();
    }

    private static IReadOnlyList<string> QueryParameters(string operationId)
    {
        var operation = Operations().Single(op => op.OperationId == operationId).Operation;
        return operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray().Where(p => p.GetProperty("in").GetString() == "query").Select(p => p.GetProperty("name").GetString()!).ToList()
            : [];
    }

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found (NRX-016)");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}
