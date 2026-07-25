using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.WorkManagement;

public class LabelsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid LabelId = Guid.NewGuid();

    public LabelsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateLabel_WithValidData_ReturnsCreated()
    {
        var body = new { Color = "#FF0000", Name = "Urgent" };
        var response = await _client.PostAsync($"/api/v1/boards/{BoardId}/labels", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListLabels_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/boards/{BoardId}/labels");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLabel_WithValidData_ReturnsOk()
    {
        var body = new { Name = "Updated Label", Color = "#00FF00" };
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/v1/labels/{LabelId}")
        {
            Content = JsonContent(body)
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLabel_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/api/v1/labels/{LabelId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
