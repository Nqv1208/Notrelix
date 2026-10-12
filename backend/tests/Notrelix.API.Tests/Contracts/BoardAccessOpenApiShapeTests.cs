using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The checked-in producer contract (backend/contracts/openapi/notrelix.v1.json,
/// regenerated and diffed by CI) exposes the Board access read: a GET on the
/// Board returning <c>BoardAccessDto</c> with the Board version, the caller's
/// level and source, the counts with their completeness flag, and the sources.
/// </summary>
public class BoardAccessOpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    [Fact]
    public void BoardAccess_IsAGetOnTheBoard_ReturningBoardAccessDto()
    {
        var operation = Document.GetProperty("paths").GetProperty("/api/v1/boards/{boardId}/access").GetProperty("get");

        operation.GetProperty("operationId").GetString().Should().Be("WorkManagement.Boards.GetAccess");
        operation.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith(".BoardAccessDto");
    }

    [Fact]
    public void BoardAccessDto_CarriesVersion_Level_Source_Counts_AndSources()
    {
        Properties("BoardAccessDto").Should().BeEquivalentTo(
            ["boardId", "version", "myLevel", "mySource", "peopleCount", "guestCount", "peopleCountComplete", "sources"]);
        Properties("BoardAccessSourceDto").Should().BeEquivalentTo(["kind", "grantedBy"]);
        Properties("BoardAccessGrantorDto").Should().BeEquivalentTo(["userId", "name"]);
        Properties("BoardAccessSourceCountDto").Should().BeEquivalentTo(["kind", "id", "name", "count"]);
    }

    private static IReadOnlyList<string> Properties(string dto) =>
        Document.GetProperty("components").GetProperty("schemas").EnumerateObject()
            .Single(s => s.Name.Split('.').Last() == dto).Value
            .GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();

    private static JsonElement Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "openapi", "notrelix.v1.json")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found (NRX-016)");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement;
    }
}
