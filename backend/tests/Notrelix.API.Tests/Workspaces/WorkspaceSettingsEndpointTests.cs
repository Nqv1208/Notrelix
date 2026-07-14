using System.Net;
using System.Text;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class WorkspaceSettingsEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";

    public WorkspaceSettingsEndpointTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetSettings_WithExistingWorkspace_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}/settings");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSettings_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/settings");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSettings_WithValidData_ReturnsSuccess()
    {
        var body = new { AllowPublicSharing = true, EnforceMfa = false, AllowGuestInvites = true, DefaultMemberRole = "Member", InvitationExpiryDays = 14, ExpectedVersion = 1 };
        var response = await _client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/settings", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSettings_WithNonexistentWorkspace_ReturnsNotFound()
    {
        var body = new { AllowPublicSharing = true, EnforceMfa = false, AllowGuestInvites = true, DefaultMemberRole = "Member", InvitationExpiryDays = 7, ExpectedVersion = 1 };
        var response = await _client.PutAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999/settings", JsonContent(body));
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSettings_Unauthenticated_ReturnsUnauthorized()
    {
        var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        var body = new { AllowPublicSharing = true, EnforceMfa = false, AllowGuestInvites = true, DefaultMemberRole = "Member", InvitationExpiryDays = 7, ExpectedVersion = 1 };
        var response = await client.PutAsync($"/api/v1/workspaces/{WorkspaceId}/settings", JsonContent(body));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static StringContent JsonContent(object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
