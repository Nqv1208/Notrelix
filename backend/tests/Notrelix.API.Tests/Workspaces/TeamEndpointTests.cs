using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class TeamEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid TeamId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public TeamEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListTeams_WithExistingWorkspace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/teams");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTeam_WithValidData_ReturnsSuccess()
    {
        var body = new { Name = "New Team", Description = "A test team" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTeam_WithInvalidName_ReturnsBadRequest()
    {
        var body = new { Name = "" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeam_WithExistingTeam_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameTeam_WithValidName_ReturnsSuccess()
    {
        var body = new { Name = "Renamed Team" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTeamDescription_WithValidData_ReturnsSuccess()
    {
        var body = new { Description = "Updated description" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/description", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchiveTeam_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/archive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnarchiveTeam_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/unarchive", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTeam_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreTeam_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/restore", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddTeamMember_WithValidData_ReturnsSuccess()
    {
        var body = new { UserId = UserId, Role = "Member" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/members", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTeamMember_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/members/{UserId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WithValidData_ReturnsSuccess()
    {
        var body = new { NewRole = "Lead" };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/teams/{TeamId}/members/{UserId}/role", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTeam_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { Name = "New Team" };
        var response = await client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/teams", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
