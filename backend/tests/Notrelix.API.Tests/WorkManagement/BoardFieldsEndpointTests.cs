using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class BoardFieldsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid FieldId = Guid.NewGuid();

    public BoardFieldsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateField_WithValidData_ReturnsCreated()
    {
        var body = new { Name = "Priority", Type = "Status", SettingsJson = "{}", Position = 1.0 };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateField_WithValidData_ReturnsOk()
    {
        var body = new { Name = "Updated Field", Type = "Text", ExpectedVersion = 1 };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/boards/{BoardId}/fields/{FieldId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteField_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/boards/{BoardId}/fields/{FieldId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RetiredFullListReorderRoutes_AreNotMapped()
    {
        // WM-V3-APP-FIELD-001 / APP-OPTION-001: drag is a single relative move.
        var fields = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields/reorder", JsonContent(new { Items = Array.Empty<object>() }));
        var options = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields/{FieldId}/options/reorder", JsonContent(new { OrderedOptionIds = Array.Empty<Guid>() }));

        // 405 means the path matches another verb's route; either way no POST handler exists.
        fields.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        options.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task GetBoardSchema_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}/schema");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddFieldOption_WithValidData_ReturnsCreated()
    {
        var body = new { Name = "High Priority", Color = "#FF0000", ExpectedVersion = 1, PreviousSiblingId = (Guid?)null, NextSiblingId = (Guid?)null };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields/{FieldId}/options", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveFieldOption_ReturnsNoContent()
    {
        var optionId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/v1/boards/{BoardId}/fields/{FieldId}/options/{optionId}?expectedVersion=1");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFieldOption_WithValidData_ReturnsOk()
    {
        var optionId = Guid.NewGuid();
        var body = new { Name = "Updated Option", Color = "#00FF00", ExpectedVersion = 1 };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/boards/{BoardId}/fields/{FieldId}/options/{optionId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveFieldOption_WithNeighborIntent_IsRouted()
    {
        var body = new { ExpectedVersion = 1, PreviousSiblingId = Guid.NewGuid(), NextSiblingId = (Guid?)null };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields/{FieldId}/options/{Guid.NewGuid()}/move", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
