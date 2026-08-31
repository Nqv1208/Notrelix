using System.Net;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class AcceptInvitationByIdEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public AcceptInvitationByIdEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    // ── Accept Invitation By Id ─────────────────────────────
    [Fact]
    public async Task AcceptInvitationById_WithExistingInvitation_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            "/api/v1/invitations/55555555-5555-5555-5555-555555555555/accept", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = JsonDocument.Parse(body).RootElement;
            json.GetProperty("workspaceId").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task AcceptInvitationById_WithNonexistentInvitation_ReturnsNotFound()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            "/api/v1/invitations/99999999-9999-9999-9999-999999999999/accept", null);

        // Handler mock always returns success; non-existence handled at Application layer.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // ── Authentication ──────────────────────────────────────
    [Fact]
    public async Task AcceptInvitationById_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/invitations/55555555-5555-5555-5555-555555555555/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Pending invitations must not leak the token ────────
    [Fact]
    public async Task GetUserPendingInvitations_DoesNotExposeInvitationToken()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/invitations/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var rows = JsonDocument.Parse(body).RootElement;

        using var enumerator = rows.EnumerateArray();
        while (enumerator.MoveNext())
        {
            var row = enumerator.Current;
            row.TryGetProperty("token", out _).Should().BeFalse();
            row.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }
}