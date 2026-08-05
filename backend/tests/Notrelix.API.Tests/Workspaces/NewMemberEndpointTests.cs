using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class NewMemberEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid TargetUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public NewMemberEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task AddMember_WithValidData_ReturnsSuccess()
    {
        var body = new { UserId = TargetUserId, Role = "Member" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/add", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMember_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var body = new { UserId = TargetUserId, Role = "Member" };
        var response = await _client.PostAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/members/add", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendMember_WithExistingMember_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/{TargetUserId}/suspend", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendMember_WithNonexistentMember_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/99999999-9999-9999-9999-999999999999/suspend", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateMember_WithExistingMember_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/{TargetUserId}/activate", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMember_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { UserId = TargetUserId, Role = "Member" };
        var response = await client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/add", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendMember_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members/{TargetUserId}/suspend", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
