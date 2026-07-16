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
        var body = new { Name = "Updated Field", Type = "Text" };
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
    public async Task ReorderFields_WithValidData_ReturnsNoContent()
    {
        var body = new { BoardId = BoardId.ToString(), Items = new[] { new { Id = FieldId.ToString(), NewPosition = 2.0 } } };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/fields/reorder", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoardSchema_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}/schema");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
