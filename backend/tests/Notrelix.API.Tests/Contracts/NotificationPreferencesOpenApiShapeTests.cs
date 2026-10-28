using System.Text.Json;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// The notification preference matrix contract (Collaboration, accepted 2026-10-23) asserted on the checked-in
/// producer contract: both operations declare their typed bodies, the matrix row carries one flag per channel
/// and the update carries its required expected version and rows.
/// </summary>
public class NotificationPreferencesOpenApiShapeTests
{
    private static readonly JsonElement Document = Load();

    [Theory]
    [InlineData("Collaboration.Preferences.GetNotificationPreferences")]
    [InlineData("Collaboration.Preferences.UpdateNotificationPreferences")]
    public void Operation_ReturnsTheTypedMatrix(string operationId)
    {
        var schema = Operation(operationId).GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");

        schema.GetProperty("$ref").GetString().Should().EndWith(".NotificationPreferencesDto");
    }

    [Fact]
    public void Matrix_ExposesVersionAndOneRowPerTypeWithEachChannel()
    {
        var matrix = Schema(".NotificationPreferencesDto").GetProperty("properties");
        matrix.GetProperty("version").GetProperty("format").GetString().Should().Be("int64");
        matrix.GetProperty("rows").GetProperty("type").GetString().Should().Be("array");

        var row = Schema(".NotificationPreferenceRowDto").GetProperty("properties");
        row.GetProperty("type").GetProperty("type").GetString().Should().Be("string");
        row.GetProperty("inApp").GetProperty("type").GetString().Should().Be("boolean");
        row.GetProperty("email").GetProperty("type").GetString().Should().Be("boolean");
    }

    [Fact]
    public void Update_CarriesExpectedVersionAndRowsInTheBody_AndDeclares412()
    {
        var operation = Operation("Collaboration.Preferences.UpdateNotificationPreferences");
        var request = Schema(".UpdateNotificationPreferencesRequest").GetProperty("properties");
        request.GetProperty("expectedVersion").GetProperty("format").GetString().Should().Be("int64");
        request.TryGetProperty("rows", out _).Should().BeTrue();

        var row = Schema(".NotificationPreferenceRowRequest").GetProperty("properties");
        row.TryGetProperty("inApp", out _).Should().BeTrue();
        row.TryGetProperty("email", out _).Should().BeTrue();

        operation.GetProperty("responses").EnumerateObject().Select(r => r.Name).Should().Contain("412");
    }

    private static JsonElement Schema(string suffix) =>
        Document.GetProperty("components").GetProperty("schemas").EnumerateObject()
            .Single(s => s.Name.EndsWith(suffix, StringComparison.Ordinal) && s.Name.Contains("Preferences", StringComparison.Ordinal)).Value;

    private static JsonElement Operation(string operationId)
    {
        foreach (var path in Document.GetProperty("paths").EnumerateObject())
            foreach (var method in path.Value.EnumerateObject())
                if (method.Value.TryGetProperty("operationId", out var id) && id.GetString() == operationId)
                    return method.Value;
        throw new InvalidOperationException($"Operation {operationId} is not in the contract.");
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
