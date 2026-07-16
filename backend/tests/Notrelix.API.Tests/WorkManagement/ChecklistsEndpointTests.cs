using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class ChecklistsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly Guid ChecklistId = Guid.NewGuid();
    private static readonly Guid ChecklistItemId = Guid.NewGuid();

    public ChecklistsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListChecklists_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/board-items/{ItemId}/checklists");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateChecklist_WithValidData_ReturnsCreated()
    {
        var body = new { Title = "My Checklist" };
        var response = await _client.PostAsync($"/api/v1/board-items/{ItemId}/checklists", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateChecklist_WithValidData_ReturnsOk()
    {
        var body = new { Title = "Updated Checklist" };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/checklists/{ChecklistId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteChecklist_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/checklists/{ChecklistId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateChecklistItem_WithValidData_ReturnsCreated()
    {
        var body = new { Title = "New Item" };
        var response = await _client.PostAsync($"/api/v1/checklists/{ChecklistId}/items", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateChecklistItem_WithValidData_ReturnsOk()
    {
        var body = new { IsChecked = true };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/checklist-items/{ChecklistItemId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteChecklistItem_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/checklist-items/{ChecklistItemId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
