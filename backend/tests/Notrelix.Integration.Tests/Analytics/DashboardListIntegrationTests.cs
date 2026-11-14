using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// The dashboard list runs on the application role. A member sees only the Active dashboards they created (private),
/// never a colleague's and never another Workspace's; archived dashboards are not listed; the list is a cursor page,
/// newest first. The owner's page is compared by id, never by a hard-coded count of other tenants.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DashboardListIntegrationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _colleague = null!;
    private WebApplicationFactory<Program>? _factory;

    public DashboardListIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-dashboard-list@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-dashboard-list@example.com", "Bbb", "hash");
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
        if (method == HttpMethod.Post && url.EndsWith("/dashboards", StringComparison.Ordinal))
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        return await client.SendAsync(request);
    }

    private async Task<Guid> CreateAsync(AppRoleTenant owner, string name)
    {
        var response = await SendAsync(owner.User, HttpMethod.Post, $"/api/v1/workspaces/{owner.WorkspaceId}/dashboards", new CreateDashboardRequest(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<CursorPage<DashboardSummaryDto>> ListAsync(User who, Guid workspaceId, string query = "")
    {
        var response = await SendAsync(who, HttpMethod.Get, $"/api/v1/workspaces/{workspaceId}/dashboards{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CursorPage<DashboardSummaryDto>>())!;
    }

    [Fact]
    public async Task The_owner_sees_their_own_active_dashboards_newest_first_in_pages()
    {
        var first = await CreateAsync(_a, "First");
        var second = await CreateAsync(_a, "Second");
        var third = await CreateAsync(_a, "Third");

        var page1 = await ListAsync(_a.User, _a.WorkspaceId, "?limit=2");
        page1.Items.Select(d => d.Id).Should().Equal(third, second);
        page1.NextCursor.Should().NotBeNullOrEmpty();

        var page2 = await ListAsync(_a.User, _a.WorkspaceId, $"?limit=2&cursor={Uri.EscapeDataString(page1.NextCursor!)}");
        page2.Items.Select(d => d.Id).Should().Equal(first);
        page2.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task An_archived_dashboard_is_not_listed_until_restored()
    {
        var id = await CreateAsync(_a, "Archived");
        (await SendAsync(_a.User, HttpMethod.Post, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards/{id}/archive", new DashboardVersionRequest(1)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListAsync(_a.User, _a.WorkspaceId)).Items.Should().NotContain(d => d.Id == id);
    }

    [Fact]
    public async Task A_colleague_in_the_same_workspace_does_not_see_someone_elses_private_dashboard()
    {
        var mine = await CreateAsync(_a, "Private to Ann");

        var colleagueView = await ListAsync(_colleague, _a.WorkspaceId);

        colleagueView.Items.Should().NotContain(d => d.Id == mine);
        (await ListAsync(_a.User, _a.WorkspaceId)).Items.Should().Contain(d => d.Id == mine);
    }

    [Fact]
    public async Task Another_workspaces_dashboards_never_appear_and_a_non_member_is_answered_like_a_made_up_workspace()
    {
        var ours = await CreateAsync(_a, "Workspace A");
        var theirs = await CreateAsync(_b, "Workspace B");

        (await ListAsync(_a.User, _a.WorkspaceId)).Items.Select(d => d.Id).Should().Contain(ours).And.NotContain(theirs);
        (await ListAsync(_b.User, _b.WorkspaceId)).Items.Select(d => d.Id).Should().Contain(theirs).And.NotContain(ours);

        var found = await SendAsync(_b.User, HttpMethod.Get, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards");
        var made = await SendAsync(_b.User, HttpMethod.Get, $"/api/v1/workspaces/{Guid.NewGuid()}/dashboards");
        made.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        found.StatusCode.Should().Be(made.StatusCode, "an existing Workspace answers like a made-up one to a non-member");
    }

    [Fact]
    public async Task A_limit_above_two_hundred_is_refused_with_bad_request()
    {
        var response = await SendAsync(_a.User, HttpMethod.Get, $"/api/v1/workspaces/{_a.WorkspaceId}/dashboards?limit=201");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
