using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
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
/// Moving, resizing and removing a widget run on the application role. The owner's changes are versioned on the
/// dashboard (a stale version is 412 and changes nothing); an invalid position or an unknown widget is refused with its
/// rule; a retry of a removal under the same Idempotency-Key replays; a colleague is answered like a made-up dashboard.
/// Widgets are seeded through the Domain because no add endpoint exists yet (UC-ANA-M06 is not built).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DashboardWidgetIntegrationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _colleague = null!;
    private Guid _dashboardId;
    private Guid _widgetId;
    private WebApplicationFactory<Program>? _factory;

    public DashboardWidgetIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-dashboard-widget@example.com", "Aaa", "hash");
        _colleague = await AppRoleMembers.AddAsync(_fixture, _a, "Col", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var now = DateTimeOffset.UtcNow;
        var dashboard = Dashboard.Create(_a.AccountId, _a.WorkspaceId, "Widgets", _a.User.Id, now);
        dashboard.AddWidget("Note", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"hello\"}"),
            WidgetPosition.Create(0, 0, 4, 2), _a.User.Id, now);
        _dashboardId = dashboard.Id;
        _widgetId = dashboard.Widgets.First().Id;

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

    private async Task<HttpResponseMessage> SendAsync(User who, HttpMethod method, string url, object? body = null, string? idempotencyKey = null)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private string WidgetUrl(Guid dashboardId, Guid widgetId) =>
        $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{dashboardId}/widgets/{widgetId}";

    private async Task<(long Version, WidgetPosition? Position, int Count)> ReadAsync()
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        var row = await ctx.Dashboards.AsNoTracking().Include(d => d.Widgets).FirstAsync(d => d.Id == _dashboardId);
        var widget = row.Widgets.FirstOrDefault(w => w.Id == _widgetId);
        return (row.Version, widget?.Position, row.Widgets.Count);
    }

    [Fact]
    public async Task The_owner_moves_a_widget_and_a_stale_or_invalid_move_changes_nothing()
    {
        var before = await ReadAsync();

        (await SendAsync(_a.User, HttpMethod.Put, WidgetUrl(_dashboardId, _widgetId) + "/position",
                new MoveDashboardWidgetRequest(2, 3, 6, 4, before.Version)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var moved = await ReadAsync();
        moved.Position.Should().Be(WidgetPosition.Create(2, 3, 6, 4));
        moved.Version.Should().Be(before.Version + 1);

        (await SendAsync(_a.User, HttpMethod.Put, WidgetUrl(_dashboardId, _widgetId) + "/position",
                new MoveDashboardWidgetRequest(0, 0, 1, 1, before.Version)))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed, "the first version is stale after the move");
        (await ReadAsync()).Position.Should().Be(WidgetPosition.Create(2, 3, 6, 4));

        (await SendAsync(_a.User, HttpMethod.Put, WidgetUrl(_dashboardId, _widgetId) + "/position",
                new MoveDashboardWidgetRequest(-1, 0, 1, 1, moved.Version)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "a negative coordinate is refused");
        (await ReadAsync()).Position.Should().Be(WidgetPosition.Create(2, 3, 6, 4));

        (await SendAsync(_a.User, HttpMethod.Put, WidgetUrl(_dashboardId, Guid.NewGuid()) + "/position",
                new MoveDashboardWidgetRequest(0, 0, 1, 1, moved.Version)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "an unknown widget is refused with its rule");
    }

    [Fact]
    public async Task The_owner_removes_a_widget_and_a_retry_with_the_same_key_replays()
    {
        var before = await ReadAsync();
        var key = Guid.NewGuid().ToString("D");
        var url = WidgetUrl(_dashboardId, _widgetId) + $"?expectedVersion={before.Version}";

        var first = await SendAsync(_a.User, HttpMethod.Delete, url, idempotencyKey: key);
        var retry = await SendAsync(_a.User, HttpMethod.Delete, url, idempotencyKey: key);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        retry.StatusCode.Should().Be(first.StatusCode, "the same Idempotency-Key replays the first answer");
        var after = await ReadAsync();
        after.Count.Should().Be(0);
        after.Version.Should().Be(before.Version + 1, "the removal happened once");
    }

    [Fact]
    public async Task A_colleague_is_answered_like_a_made_up_dashboard_and_the_widget_stays()
    {
        var before = await ReadAsync();

        var found = await SendAsync(_colleague, HttpMethod.Put, WidgetUrl(_dashboardId, _widgetId) + "/position",
            new MoveDashboardWidgetRequest(1, 1, 1, 1, before.Version));
        var made = await SendAsync(_colleague, HttpMethod.Put, WidgetUrl(Guid.NewGuid(), _widgetId) + "/position",
            new MoveDashboardWidgetRequest(1, 1, 1, 1, before.Version));

        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "a colleague cannot change someone else's dashboard");
        (await ReadAsync()).Position.Should().Be(WidgetPosition.Create(0, 0, 4, 2));
    }
}
