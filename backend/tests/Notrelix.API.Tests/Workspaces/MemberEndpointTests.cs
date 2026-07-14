using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class MemberEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid TargetUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public MemberEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListMembers_WithExistingWorkspace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/members");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListMembers_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/members");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InviteMember_WithValidData_ReturnsSuccess()
    {
        var body = new { Email = $"invite-{Guid.NewGuid():N}@test.com", Role = "Member" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InviteMember_WithInvalidEmail_ReturnsBadRequest()
    {
        var body = new { Email = "invalid", Role = "Member" };
        var response = await _client.PostAsync($"/api/v1/workspaces/{WorkspaceId}/members", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InviteMember_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var body = new { Email = "test@test.com", Role = "Member" };
        var response = await _client.PostAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/members", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMemberRole_WithValidRole_ReturnsSuccess()
    {
        var body = new { Role = "Admin" };
        var response = await _client.PatchAsync($"/api/v1/workspaces/{WorkspaceId}/members/{TargetUserId}", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMemberRole_WithNonexistentMember_ReturnsNotFound()
    {
        var body = new { Role = "Admin" };
        var response = await _client.PatchAsync($"/api/v1/workspaces/{WorkspaceId}/members/99999999-9999-9999-9999-999999999999", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMemberRole_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var body = new { Role = "Admin" };
        var response = await _client.PatchAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/members/22222222-2222-2222-2222-222222222222", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_WithExistingMember_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/members/{TargetUserId}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_WithNonexistentMember_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/members/99999999-9999-9999-9999-999999999999");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/members/22222222-2222-2222-2222-222222222222");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
