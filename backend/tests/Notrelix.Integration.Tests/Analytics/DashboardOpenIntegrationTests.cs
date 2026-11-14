using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// Opening one dashboard runs on the application role. The creator gets the dashboard with its widgets; a colleague in
/// the same Workspace, a person outside it and a made-up id are answered alike (the status is compared, never
/// hard-coded, and the bodies are not compared because each answer carries its own correlation id).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DashboardOpenIntegrationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _colleague = null!;
    private Guid _dashboardId;
    private WebApplicationFactory<Program>? _factory;

    public DashboardOpenIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-dashboard-open@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-dashboard-open@example.com", "Bbb", "hash");
        _colleague = await AppRoleMembers.AddAsync(_fixture, _a, "Col", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var now = DateTimeOffset.UtcNow;
        var dashboard = Dashboard.Create(_a.AccountId, _a.WorkspaceId, "Open me", _a.User.Id, now);
        dashboard.AddWidget("Status note", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"hello\"}"),
            WidgetPosition.Create(0, 0, 4, 2), _a.User.Id, now);
        _dashboardId = dashboard.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Dashboards.Add(dashboard);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> GetAsync(User who, string url)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return await client.GetAsync(url);
    }

    [Fact]
    public async Task The_creator_opens_the_dashboard_with_its_widgets()
    {
        var response = await GetAsync(_a.User, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{_dashboardId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = (await response.Content.ReadFromJsonAsync<DashboardDetailDto>())!;
        dashboard.Id.Should().Be(_dashboardId);
        dashboard.Name.Should().Be("Open me");
        dashboard.Status.Should().Be("Active");
        dashboard.Widgets.Should().ContainSingle();
        dashboard.Widgets[0].Title.Should().Be("Status note");
        dashboard.Widgets[0].Type.Should().Be("TextWidget");
        dashboard.Widgets[0].W.Should().Be(4);
        dashboard.Widgets[0].Config.GetProperty("content").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task A_colleague_is_answered_like_a_made_up_dashboard()
    {
        var found = await GetAsync(_colleague, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{_dashboardId}");
        var made = await GetAsync(_colleague, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{Guid.NewGuid()}");

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "a colleague cannot open someone else's private dashboard");
        found.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_person_outside_the_workspace_is_answered_like_a_made_up_workspace()
    {
        var found = await GetAsync(_b.User, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{_dashboardId}");
        var made = await GetAsync(_b.User, $"/api/v1/workspaces/{Guid.NewGuid()}/dashboards/{Guid.NewGuid()}");

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "an existing Workspace answers like a made-up one to a non-member");
    }
}
