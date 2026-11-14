using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// Creating a dashboard runs on the application role: the owner's create persists one private dashboard owned by the
/// caller, a retry under the same Idempotency-Key returns the first dashboard, and a person outside the Workspace is
/// answered like a made-up Workspace (the status is compared, never hard-coded).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DashboardCreateIntegrationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private WebApplicationFactory<Program>? _factory;

    public DashboardCreateIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-dashboard-create@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-dashboard-create@example.com", "Bbb", "hash");
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> PostAsync(AppRoleTenant who, Guid workspaceId, string name, string idempotencyKey)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who.User);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{workspaceId}/dashboards")
        {
            Content = JsonContent.Create(new CreateDashboardRequest(name)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task The_owner_creates_one_private_dashboard_and_a_retry_returns_the_first()
    {
        var key = Guid.NewGuid().ToString("D");

        var first = await PostAsync(_a, _a.WorkspaceId, "Weekly", key);
        var retry = await PostAsync(_a, _a.WorkspaceId, "Weekly", key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        retry.StatusCode.Should().Be(first.StatusCode);
        (await retry.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        var rows = await ctx.Dashboards.Where(d => d.WorkspaceId == _a.WorkspaceId && d.Name == "Weekly").ToListAsync();
        rows.Should().ContainSingle("the same Idempotency-Key never creates a second dashboard");
        rows[0].AccountId.Should().Be(_a.AccountId);
        rows[0].Visibility.Should().Be(Notrelix.Domain.Analytics.Dashboards.DashboardVisibility.Private);
        rows[0].Status.Should().Be(Notrelix.Domain.Analytics.Dashboards.DashboardStatus.Active);
    }

    [Fact]
    public async Task A_person_outside_the_workspace_is_answered_like_a_made_up_workspace()
    {
        var found = await PostAsync(_b, _a.WorkspaceId, "Intruder", Guid.NewGuid().ToString("D"));
        var made = await PostAsync(_b, Guid.NewGuid(), "Intruder", Guid.NewGuid().ToString("D"));

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "an existing Workspace answers like a made-up one to a non-member");
        found.StatusCode.Should().NotBe(HttpStatusCode.Created);
    }
}
