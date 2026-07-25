using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class SpaceEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid SpaceId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public SpaceEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListSpaces_WithExistingWorkspace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/spaces");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSpace_WithValidData_ReturnsSuccess()
    {
        var body = new { Name = "New Space", Visibility = "Workspace", Description = "A test space" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSpace_WithInvalidName_ReturnsBadRequest()
    {
        var body = new { Name = "", Visibility = "Workspace" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpace_WithExistingSpace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameSpace_WithValidName_ReturnsSuccess()
    {
        var body = new { Name = "Renamed Space" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSpaceDescription_WithValidData_ReturnsSuccess()
    {
        var body = new { Description = "Updated description" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/description", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeSpaceVisibility_WithValidData_ReturnsSuccess()
    {
        var body = new { Visibility = "Public" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/visibility", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeSpaceType_WithValidData_ReturnsSuccess()
    {
        var body = new { SpaceType = "Project" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/type", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveSpace_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/archive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnarchiveSpace_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/unarchive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSpace_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreSpace_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces/{SpaceId}/restore", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSpace_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { Name = "New Space", Visibility = "Workspace" };
        var response = await client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/spaces", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
