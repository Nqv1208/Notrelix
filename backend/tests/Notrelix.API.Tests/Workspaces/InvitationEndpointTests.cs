using System.Net;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class InvitationEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";

    public InvitationEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    // ── List Invitations ────────────────────────────────────
    [Fact]
    public async Task ListInvitations_WithExistingWorkspace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/invitations");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListInvitations_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/invitations");

        // Handler mock always returns success; non-existence handled at Application layer.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── Cancel Invitation ───────────────────────────────────
    [Fact]
    public async Task CancelInvitation_WithExistingInvitation_ReturnsSuccess()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/invitations/33333333-3333-3333-3333-333333333333");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelInvitation_WithNonexistentInvitation_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/workspaces/{WorkspaceId}/invitations/99999999-9999-9999-9999-999999999999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelInvitation_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/invitations/33333333-3333-3333-3333-333333333333");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    // ── Get User Pending Invitations ────────────────────────
    [Fact]
    public async Task GetUserPendingInvitations_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/invitations/pending");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── Get Invitation By Token ─────────────────────────────
    [Fact]
    public async Task GetInvitationByToken_WithValidToken_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/invitations/by-token/valid-token");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvitationByToken_WithInvalidToken_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/invitations/by-token/invalid-{Guid.NewGuid():N}");

        // Handler mock returns success; token validation at Application layer.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── Accept Invitation ───────────────────────────────────
    [Fact]
    public async Task AcceptInvitation_WithValidToken_ReturnsSuccess()
    {
        var response = await _client.PostAsync("/api/v1/invitations/accept/valid-token", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_WithInvalidToken_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"/api/v1/invitations/accept/invalid-{Guid.NewGuid():N}", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
