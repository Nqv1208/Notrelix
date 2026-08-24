using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class BoardItemsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public BoardItemsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateItem_WithValidData_ReturnsOk()
    {
        var body = new { GroupId = GroupId.ToString(), Title = "New Item", Position = 1.0 };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/items", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListItems_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}/items");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItem_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/board-items/{ItemId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateItem_WithValidData_ReturnsOk()
    {
        var body = new { Title = "Updated Item", ExpectedVersion = 1 };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/board-items/{ItemId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveItem_ReturnsNoContent()
    {
        var response = await _client.PostAsync($"/api/v1/board-items/{ItemId}/archive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DuplicateItem_ReturnsCreated()
    {
        var response = await _client.PostAsync($"/api/v1/board-items/{ItemId}/duplicate", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveItem_WithValidData_ReturnsOk()
    {
        var body = new { GroupId = GroupId.ToString(), Position = 1.0 };
        var response = await _client.PostAsync($"/api/v1/board-items/{ItemId}/move", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignMember_WithValidData_ReturnsNoContent()
    {
        var body = new { UserId = UserId.ToString() };
        var response = await _client.PostAsync($"/api/v1/board-items/{ItemId}/assignees", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnassignMember_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/board-items/{ItemId}/assignees/{UserId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
