using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The board read contract (full board, item summary, item detail) grows only
/// additively: every earlier property is still present, each new property is
/// optional and nullable so an old web or mobile bundle keeps parsing the response,
/// and every mutable read DTO still exposes its version. Asserted on the checked-in
/// producer contract (backend/contracts/openapi/notrelix.v1.json).
/// </summary>
public class BoardReadContractOpenApiTests
{
    private static readonly JsonElement Schemas = Load().GetProperty("components").GetProperty("schemas");

    private static readonly string[] ItemValueProperties =
        ["itemKey", "parentItemId", "statusOptionId", "priorityOptionId", "dueAt", "startedAt", "completedAt", "childProgress"];

    [Theory]
    [InlineData("FullBoardDto", "id,workspaceId,title,description,background,visibility,columns,lists,members,version")]
    [InlineData("BoardFieldDto", "id,boardId,name,fieldType,settings,defaultValue,position,isSystem,isDeleted,version")]
    [InlineData("BoardItemSummaryDto", "id,name,memberCount,members,labels,checklistProgress,checklistTotal,commentCount,attachmentCount,position,createdAt,updatedAt,version")]
    [InlineData("BoardItemDto", "id,boardId,workspaceId,groupId,name,members,labels,checklists,commentCount,attachmentCount,position,createdAt,updatedAt,version")]
    public void EarlierProperties_AreAllStillPresent(string dto, string earlier)
    {
        Properties(dto).Should().Contain(earlier.Split(','), $"{dto} keeps every earlier field for old clients");
    }

    [Theory]
    [InlineData("FullBoardDto", "spaceId,itemKeyPrefix")]
    [InlineData("BoardFieldDto", "options")]
    [InlineData("BoardItemSummaryDto", "itemKey,parentItemId,statusOptionId,priorityOptionId,dueAt,startedAt,completedAt,childProgress")]
    [InlineData("BoardItemDto", "itemKey,parentItemId,statusOptionId,priorityOptionId,dueAt,startedAt,completedAt,childProgress")]
    public void NewProperties_AreOptionalAndNullable(string dto, string added)
    {
        var schema = Schema(dto);
        foreach (var name in added.Split(','))
        {
            schema.GetProperty("properties").TryGetProperty(name, out var property).Should().BeTrue($"{dto}.{name} is part of the board read contract");
            // An object reference cannot carry "nullable" beside "$ref" in this
            // document; it is optional through its absence from "required".
            (IsNullable(property) || property.TryGetProperty("$ref", out _)).Should().BeTrue(
                $"{dto}.{name} must be nullable for old producers' data and old clients");
            Required(schema).Should().NotContain(name, $"{dto}.{name} must stay optional");
        }
    }

    [Fact]
    public void ItemSummaryAndDetail_CarryTheSameItemValues()
    {
        Properties("BoardItemSummaryDto").Should().Contain(ItemValueProperties);
        Properties("BoardItemDto").Should().Contain(ItemValueProperties);
        Properties("BoardItemDto").Should().Contain("descriptionMd", "an Item carries its Markdown description");
        Required(Schema("BoardItemDto")).Should().NotContain("descriptionMd", "an item without a description omits it");
    }

    [Fact]
    public void FieldOptions_CarryIdentityLabelColorOrderStatusCategoryAndItemCount()
    {
        Properties("BoardFieldOptionDto").Should().BeEquivalentTo(["id", "name", "color", "position", "category", "itemCount"]);
        IsNullable(Schema("BoardFieldOptionDto").GetProperty("properties").GetProperty("category")).Should().BeTrue(
            "only options of a Status field carry a category");
        Schema("BoardFieldOptionDto").GetProperty("properties").GetProperty("itemCount").GetProperty("type").GetString()
            .Should().Be("integer", "the count of live items holding the option");
    }

    [Fact]
    public void ChildProgress_IsADoneOverTotalCount()
    {
        Properties("BoardItemChildProgressDto").Should().BeEquivalentTo(["done", "total"]);
        foreach (var name in new[] { "done", "total" })
            Schema("BoardItemChildProgressDto").GetProperty("properties").GetProperty(name).GetProperty("type").GetString()
                .Should().Be("integer");
    }

    [Theory]
    [InlineData("FullBoardDto")]
    [InlineData("BoardFieldDto")]
    [InlineData("BoardItemSummaryDto")]
    [InlineData("BoardItemDto")]
    public void EveryMutableReadDto_StillExposesVersion(string dto)
    {
        Properties(dto).Should().Contain("version");
    }

    private static JsonElement Schema(string dto) =>
        Schemas.EnumerateObject().Single(s => s.Name.EndsWith("." + dto, StringComparison.Ordinal)).Value;

    private static IReadOnlyList<string> Properties(string dto) =>
        Schema(dto).GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();

    private static IReadOnlyList<string> Required(JsonElement schema) =>
        schema.TryGetProperty("required", out var required) ? required.EnumerateArray().Select(r => r.GetString()!).ToList() : [];

    private static bool IsNullable(JsonElement property)
    {
        if (property.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean()) return true;
        if (property.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.Array)
            return type.EnumerateArray().Any(t => t.GetString() == "null");
        foreach (var combinator in new[] { "oneOf", "anyOf", "allOf" })
        {
            if (property.TryGetProperty(combinator, out var options)
                && options.EnumerateArray().Any(o => IsNullable(o) || (o.TryGetProperty("type", out var t) && t.GetString() == "null")))
                return true;
        }

        return false;
    }

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}
