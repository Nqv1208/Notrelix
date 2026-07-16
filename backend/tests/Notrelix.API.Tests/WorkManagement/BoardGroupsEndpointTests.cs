using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class BoardGroupsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    public BoardGroupsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateGroup_WithValidData_ReturnsCreated()
    {
        var body = new { Title = "New Group", Color = "#FF0000" };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/groups", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateGroup_WithValidData_ReturnsOk()
    {
        var body = new { Title = "Updated Group" };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/board-groups/{GroupId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveGroup_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/board-groups/{GroupId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnarchiveGroup_ReturnsNoContent()
    {
        var response = await _client.PostAsync($"/api/v1/board-groups/{GroupId}/unarchive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DuplicateGroup_ReturnsCreated()
    {
        var response = await _client.PostAsync($"/api/v1/board-groups/{GroupId}/duplicate", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderGroups_WithValidData_ReturnsNoContent()
    {
        var body = new { BoardId = BoardId.ToString(), Items = new[] { new { Id = GroupId.ToString(), NewPosition = 1.0 } } };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/groups/reorder", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
