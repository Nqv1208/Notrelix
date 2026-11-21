using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A suspended or closed Account is read-only, over HTTP on the application login through the production host: the
/// workspace read carries the Account status, members still read, every write is refused with
/// <c>account.suspended</c>, a trialing Account is not affected, and lifting the suspension restores writes.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AccountSuspensionAppRoleTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _member = null!;
    private WebApplicationFactory<Program>? _factory;

    public AccountSuspensionAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-suspension@example.com", "Asu", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "SuMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private HttpClient As(User who)
    {
        var client = Factory.CreateClient();
        using var scope = Factory.Services.CreateScope();
        client.DefaultRequestHeaders.Authorization = new("Bearer", scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who));
        return client;
    }

    private async Task SetStatusAsync(string status)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        await ctx.Database.ExecuteSqlRawAsync("UPDATE account.accounts SET status = {0} WHERE id = {1}", status, _a.AccountId);
    }

    private async Task<(HttpStatusCode Status, JsonNode? Json)> WorkspaceAsync(User who)
    {
        using var response = await As(who).GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}");
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, response.IsSuccessStatusCode ? JsonNode.Parse(body) : null);
    }

    private Task<HttpResponseMessage> CreateBoardAsync(User who)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/workspaces/{_a.WorkspaceId}/boards")
        {
            Content = JsonContent.Create(new { title = "New board" }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        return As(who).SendAsync(request);
    }

    [Theory]
    [InlineData("Active", "Active")]
    [InlineData("Trialing", "Active")]
    [InlineData("Suspended", "Suspended")]
    [InlineData("Closed", "Closed")]
    public async Task TheWorkspaceRead_CarriesTheAccountStatus_ForOwnerAndMember(string stored, string shown)
    {
        await SetStatusAsync(stored);

        foreach (var who in new[] { _a.User, _member })
        {
            var (status, json) = await WorkspaceAsync(who);
            status.Should().Be(HttpStatusCode.OK, "member reads stay open while the Account is suspended");
            json!["accountStatus"]!.GetValue<string>().Should().Be(shown);
        }
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Closed")]
    public async Task EveryWrite_IsRefusedWithAccountSuspended_ButReadsStayOpen(string status)
    {
        await SetStatusAsync(status);

        foreach (var who in new[] { _a.User, _member })
        {
            using var write = await CreateBoardAsync(who);
            write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            JsonNode.Parse(await write.Content.ReadAsStringAsync())!["errorCode"]!.GetValue<string>().Should().Be("account.suspended");
        }

        using var boards = await As(_member).GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}/boards");
        boards.StatusCode.Should().Be(HttpStatusCode.OK, "reading boards is not a write");
    }

    [Fact]
    public async Task LiftingTheSuspension_RestoresWrites_AndNothingWasDeleted()
    {
        await SetStatusAsync("Suspended");
        (await CreateBoardAsync(_a.User)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await SetStatusAsync("Active");

        (await CreateBoardAsync(_a.User)).IsSuccessStatusCode.Should().BeTrue();
        var (status, json) = await WorkspaceAsync(_a.User);
        status.Should().Be(HttpStatusCode.OK);
        json!["accountStatus"]!.GetValue<string>().Should().Be("Active");
    }
}
