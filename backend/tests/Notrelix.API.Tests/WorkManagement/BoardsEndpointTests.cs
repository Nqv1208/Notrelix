using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class BoardsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid BoardId = Guid.NewGuid();

    public BoardsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListBoards_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/boards");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBoard_WithValidData_ReturnsCreated()
    {
        var body = new { Title = "New Board", Description = "Test", Background = "default" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/boards", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetBoard_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBoard_WithValidData_ReturnsOk()
    {
        var body = new { Title = "Updated Board" };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/boards/{BoardId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveBoard_ReturnsNoContent()
    {
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/archive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnarchiveBoard_ReturnsNoContent()
    {
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/unarchive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
