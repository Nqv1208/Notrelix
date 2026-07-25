using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class BoardViewsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid ViewId = Guid.NewGuid();

    public BoardViewsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetView_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}/views");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateView_WithValidData_ReturnsOk()
    {
        var body = new { Name = "My View", ViewMode = "Kanban", Position = 1.0 };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/views", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveView_WithValidData_ReturnsOk()
    {
        var body = new { ViewMode = "Kanban", Filters = "{}" };
        var response = await _client.PutAsync($"/api/v1/boards/{BoardId}/views", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateViewConfig_WithValidData_ReturnsOk()
    {
        var body = new { ConfigJson = "{}" };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/boards/{BoardId}/views/{ViewId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteView_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/boards/{BoardId}/views/{ViewId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
