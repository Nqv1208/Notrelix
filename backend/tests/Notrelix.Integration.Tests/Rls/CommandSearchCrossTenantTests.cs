using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-WSP-N06 over HTTP on the application role: a member of tenant B who searches the command palette with tenant
/// A's Workspace id gets the same answer as for a made-up id, and none of tenant A's Board titles come back (ADR-010:
/// command search never returns what the caller cannot read, and a Workspace the caller does not belong to is not
/// disclosed by its existence).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CommandSearchCrossTenantTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private WebApplicationFactory<Program>? _factory;

    public CommandSearchCrossTenantTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-search@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-search@example.com", "Bbb", "hash");
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body)> AsAsync(User who, string url)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync(url);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    // The body may echo the requested id or a trace id; compare what the answer says, not which id it was asked about.
    private static string Normalize(string body) =>
        Regex.Replace(body, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}", "<id>");

    [Fact]
    public async Task MemberOfAnotherTenant_SearchingWithTenantAWorkspaceId_GetsTheIdenticalNotFound_AndNoBoardTitle()
    {
        var query = "q=Aaa%20Roadmap";

        var found = await AsAsync(_b.User, $"/api/v1/workspaces/{_a.WorkspaceId}/command-search?{query}");
        var made = await AsAsync(_b.User, $"/api/v1/workspaces/{Guid.NewGuid()}/command-search?{query}");

        made.Status.Should().Be(HttpStatusCode.NotFound, "a made-up workspace is NotFound");
        found.Should().Be(made, "a Workspace the caller does not belong to answers like a made-up one");
        found.Body.Should().NotContain("Aaa Roadmap", "no Board of another tenant may leak through command search");
    }
}
