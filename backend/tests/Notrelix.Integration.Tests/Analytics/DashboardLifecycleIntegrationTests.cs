using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// Archive, restore and delete of a dashboard run on the application role. The owner moves the dashboard through
/// its versions (a stale expected version is 412 and changes nothing); another member of the Workspace and a person
/// outside it are answered like a made-up dashboard or Workspace (the status is compared, never hard-coded).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DashboardLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _colleague = null!;
    private WebApplicationFactory<Program>? _factory;

    public DashboardLifecycleIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-dashboard-lifecycle@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-dashboard-lifecycle@example.com", "Bbb", "hash");
        _colleague = await AppRoleMembers.AddAsync(_fixture, _a, "Col", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> SendAsync(User who, HttpMethod method, string url, object? body = null)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private async Task<Guid> CreateAsync(AppRoleTenant owner, string name)
    {
        var response = await SendAsync(owner.User, HttpMethod.Post, $"/api/v1/workspaces/{owner.WorkspaceId}/dashboards", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private string ById(AppRoleTenant workspaceOf, Guid dashboardId) =>
        $"/api/v1/workspaces/{workspaceOf.WorkspaceId}/dashboards/{dashboardId}";

    private async Task<(bool Exists, DashboardStatus? Status, long? Version)> ReadAsync(Guid dashboardId)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        var row = await ctx.Dashboards.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dashboardId);
        return row is null ? (false, null, null) : (true, row.Status, row.Version);
    }

    [Fact]
    public async Task The_owner_archives_restores_and_deletes_and_a_stale_version_changes_nothing()
    {
        var id = await CreateAsync(_a, "Lifecycle");

        (await SendAsync(_a.User, HttpMethod.Post, ById(_a, id) + "/archive", new DashboardVersionRequest(1)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archived = await ReadAsync(id);
        archived.Status.Should().Be(DashboardStatus.Archived);
        archived.Version.Should().Be(2);

        (await SendAsync(_a.User, HttpMethod.Post, ById(_a, id) + "/archive", new DashboardVersionRequest(1)))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed, "version 1 is stale after the archive");

        (await SendAsync(_a.User, HttpMethod.Post, ById(_a, id) + "/restore", new DashboardVersionRequest(2)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var restored = await ReadAsync(id);
        restored.Status.Should().Be(DashboardStatus.Active);
        restored.Version.Should().Be(3);

        (await SendAsync(_a.User, HttpMethod.Delete, ById(_a, id) + "?expectedVersion=2"))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed, "version 2 is stale, the dashboard is still there");
        (await ReadAsync(id)).Exists.Should().BeTrue();

        (await SendAsync(_a.User, HttpMethod.Delete, ById(_a, id) + "?expectedVersion=3"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAsync(id)).Exists.Should().BeFalse("a deleted dashboard is gone");
    }

    [Fact]
    public async Task Another_member_of_the_workspace_is_answered_like_a_made_up_dashboard()
    {
        var id = await CreateAsync(_a, "Private");

        var found = await SendAsync(_colleague, HttpMethod.Post, ById(_a, id) + "/archive", new DashboardVersionRequest(1));
        var made = await SendAsync(_colleague, HttpMethod.Post, ById(_a, Guid.NewGuid()) + "/archive", new DashboardVersionRequest(1));

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "a colleague cannot tell someone else's dashboard from a missing one");
        (await ReadAsync(id)).Status.Should().Be(DashboardStatus.Active, "the colleague did not archive it");
    }

    [Fact]
    public async Task A_person_outside_the_workspace_is_answered_like_a_made_up_workspace()
    {
        var id = await CreateAsync(_a, "Outside");

        var found = await SendAsync(_b.User, HttpMethod.Delete, ById(_a, id) + "?expectedVersion=1");
        var made = await SendAsync(_b.User, HttpMethod.Delete, $"/api/v1/workspaces/{Guid.NewGuid()}/dashboards/{Guid.NewGuid()}?expectedVersion=1");

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "an existing Workspace answers like a made-up one to a non-member");
        (await ReadAsync(id)).Exists.Should().BeTrue();
    }
}
