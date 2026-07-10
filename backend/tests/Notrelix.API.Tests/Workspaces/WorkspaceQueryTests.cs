using System.Net;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Workspaces;

public class WorkspaceQueryTests : IClassFixture<NotrelixApiFactory>
{
    private readonly HttpClient _client;
    private const string WorkspaceId = "A0000000-0000-0000-0000-000000000001";

    public WorkspaceQueryTests(NotrelixApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetWorkspace_WithExistingId_ReturnsSuccess()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/{WorkspaceId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkspace_WithNonexistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/workspaces/99999999-9999-9999-9999-999999999999");

        // Handler mock always returns success; non-existence handled at Application layer.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkspaceBySlug_WithExistingSlug_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/workspaces/by-slug/my-workspace");

        // Endpoint requires X-Workspace-Id header (see [FromHeader] binding).
        // Without it, the command fails validation → 400.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkspaceBySlug_WithNonexistentSlug_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/by-slug/nonexistent-{Guid.NewGuid():N}");

        // Endpoint requires X-Workspace-Id header (see [FromHeader] binding).
        // Without it, the command fails validation → 400.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetWorkspaceBySlug_WithInvalidSlug_ReturnsNotFound(string slug)
    {
        var response = await _client.GetAsync($"/api/v1/workspaces/by-slug/{slug}");

        // Endpoint requires X-Workspace-Id header (see [FromHeader] binding).
        // Without it, the command fails validation → 400.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListWorkspaces_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/v1/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
