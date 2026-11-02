using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The item edit (PATCH /board-items/{itemId}: title, description, dates) is an idempotent operation like
/// its sibling item writes: the checked-in producer contract names the Idempotency-Key header, the replay
/// header and the 409 for a key reused with another payload. Without the header the typed 400 of the shared
/// idempotency filter answers (see IdempotencyEndpointContractTests).
/// </summary>
public class ItemEditIdempotencyOpenApiTests
{
    private static JsonElement Operation(string operationId) =>
        Load().GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Select(method => method.Value)
            .Single(operation => operation.ValueKind == JsonValueKind.Object
                && operation.TryGetProperty("operationId", out var id)
                && id.GetString() == operationId);

    [Fact]
    public void TheItemEdit_NamesTheIdempotencyKeyHeader()
    {
        var parameters = Operation("WorkManagement.BoardItems.Update").GetProperty("parameters").EnumerateArray().ToList();

        var key = parameters.Single(p => p.GetProperty("name").GetString() == "Idempotency-Key");
        key.GetProperty("in").GetString().Should().Be("header");
        key.GetProperty("required").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void TheItemEdit_DeclaresTheReplayHeader_AndTheKeyReuseConflict()
    {
        var responses = Operation("WorkManagement.BoardItems.Update").GetProperty("responses");

        responses.GetProperty("200").GetProperty("headers").TryGetProperty("Idempotency-Replayed", out _).Should().BeTrue();
        responses.TryGetProperty("409", out _).Should().BeTrue("a key reused with another payload is a conflict");
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
