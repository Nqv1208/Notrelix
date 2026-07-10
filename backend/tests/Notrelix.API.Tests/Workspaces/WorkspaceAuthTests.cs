using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class WorkspaceAuthTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public WorkspaceAuthTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    private const string BasePath = "/api/v1/workspaces";
    private const string AccountId = "A0000000-0000-0000-0000-000000000001";
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";

    // ── Public endpoints do not require auth ─────────────────
    // (none — all workspace endpoints require auth)

    // ── All workspace endpoints require auth ─────────────────
    [Fact]
    public async Task ListWorkspaces_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkspace_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var body = JsonContent(new { Name = "Test Workspace", Description = "Test", IsPersonal = false });

        var response = await client.PostAsync($"/api/v1/accounts/{AccountId}/workspaces", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWorkspace_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{WorkspaceId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateWorkspaceProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var body = JsonContent(new { Name = "Updated", Description = "Updated desc" });

        var response = await client.PatchAsync($"{BasePath}/{WorkspaceId}/profile", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ArchiveWorkspace_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BasePath}/{WorkspaceId}/archive?expectedVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RestoreWorkspace_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BasePath}/{WorkspaceId}/restore?expectedVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWorkspaceBySlug_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/by-slug/test-slug");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Member endpoints require auth ────────────────────────
    [Fact]
    public async Task ListMembers_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{WorkspaceId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InviteMember_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var body = JsonContent(new { Email = "test@test.com", Role = "Member" });

        var response = await client.PostAsync($"{BasePath}/{WorkspaceId}/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMemberRole_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var body = JsonContent(new { Role = "Admin" });

        var response = await client.PatchAsync($"{BasePath}/{WorkspaceId}/members/22222222-2222-2222-2222-222222222222", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveMember_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{BasePath}/{WorkspaceId}/members/22222222-2222-2222-2222-222222222222");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Invitation endpoints require auth ────────────────────
    [Fact]
    public async Task ListInvitations_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{WorkspaceId}/invitations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelInvitation_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{BasePath}/{WorkspaceId}/invitations/33333333-3333-3333-3333-333333333333");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserPendingInvitations_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/invitations/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvitation_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/invitations/accept/test-token", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInvitationByToken_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/invitations/by-token/test-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated endpoints should pass auth check ───────
    [Fact]
    public async Task ListWorkspaces_Authenticated_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"{BasePath}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkspace_Authenticated_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = JsonContent(new { Name = "API Test Workspace", Description = "Created in test", IsPersonal = false });

        var response = await client.PostAsync($"/api/v1/accounts/{AccountId}/workspaces", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    // ── Helper ──────────────────────────────────────────────
    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
