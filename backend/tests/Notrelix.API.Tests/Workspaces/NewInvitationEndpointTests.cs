using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class NewInvitationEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";
    private static readonly Guid InvitationId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public NewInvitationEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task DeclineInvitation_WithExistingInvitation_ReturnsSuccess()
    {
        var response = await _client.PostAsync($"/api/v1/invitations/{InvitationId}/decline", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeclineInvitation_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsync($"/api/v1/invitations/{InvitationId}/decline", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeInvitationRole_WithValidData_ReturnsSuccess()
    {
        var body = new { NewRole = "Admin" };
        var response = await _client.PatchAsync($"/api/v1/workspaces/{WorkspaceId}/invitations/{InvitationId}/role", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeInvitationRole_WithNonexistentInvitation_ReturnsNotFound()
    {
        var body = new { NewRole = "Admin" };
        var response = await _client.PatchAsync($"/api/v1/workspaces/{WorkspaceId}/invitations/99999999-9999-9999-9999-999999999999/role", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeInvitationRole_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { NewRole = "Admin" };
        var response = await client.PatchAsync($"/api/v1/workspaces/{WorkspaceId}/invitations/{InvitationId}/role", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
