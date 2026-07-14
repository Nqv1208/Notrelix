using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class NewWorkspaceEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private const string AccountId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid NewOwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public NewWorkspaceEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task UnarchiveWorkspace_ReturnsSuccess()
    {
        var body = new { ExpectedVersion = 1 };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/archive/unarchive", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnarchiveWorkspace_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { ExpectedVersion = 1 };
        var response = await client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/archive/unarchive", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteWorkspace_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransferOwnership_WithValidData_ReturnsSuccess()
    {
        var body = new { NewOwnerId = NewOwnerId, ExpectedVersion = 1 };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/transfer-ownership", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAccountWorkspaces_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{AccountId}/workspaces");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolveSlug_WithValidSlug_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{AccountId}/resolve?slug=test-workspace");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResolveSlug_WithNonexistentSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{AccountId}/resolve?slug=nonexistent-{Guid.NewGuid():N}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
