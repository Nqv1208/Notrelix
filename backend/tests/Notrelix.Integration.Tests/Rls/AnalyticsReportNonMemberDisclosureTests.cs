using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The two report read endpoints (board report and workspace work report) answer a person outside the tenant the
/// same way for a report that exists and for an id that never existed (NRX-004). Proven over the production graph
/// on the application role for a person of another account and a person of the same account who holds no
/// Workspace membership. The status is compared, not hard-coded: the refusal may be NotFound or Forbidden, and the
/// bodies are not compared because each answer carries its own correlation id.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AnalyticsReportNonMemberDisclosureTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _sameAccountOutsider = null!;
    private User _unrelated = null!;
    private WebApplicationFactory<Program>? _factory;

    public AnalyticsReportNonMemberDisclosureTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-analytics-nonmember@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-analytics-nonmember@example.com", "Bbb", "hash");
        _unrelated = await AppRoleUsers.AddAsync(_fixture, "unrelated-analytics-nonmember@example.com", "Unr", "hash");

        _sameAccountOutsider = await AppRoleUsers.AddAsync(_fixture, "outsider-analytics-nonmember@example.com", "Out", "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _sameAccountOutsider.Id, AccountRole.Member, _a.User.Id, Now));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(HttpStatusCode Status, string Body)> GetAsync(User who, string url)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task AssertRefusedIdenticallyAsync(User who, string existingUrl, string madeUpUrl, string because)
    {
        var found = await GetAsync(who, existingUrl);
        var made = await GetAsync(who, madeUpUrl);

        made.Status.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }, $"{because}: a made-up id is refused");
        found.Status.Should().Be(made.Status, $"{because}: an existing report answers like a made-up one");
    }

    [Fact]
    public async Task BoardReport_OfAnotherAccount_AnswersLikeAMadeUpBoard()
    {
        await AssertRefusedIdenticallyAsync(
            _b.User,
            $"/api/v1/boards/{_a.BoardId}/report",
            $"/api/v1/boards/{Guid.NewGuid()}/report",
            "a person of another account");
    }

    [Fact]
    public async Task WorkspaceReport_OfAnotherAccount_AnswersLikeAMadeUpWorkspace()
    {
        await AssertRefusedIdenticallyAsync(
            _b.User,
            $"/api/v1/workspaces/{_a.WorkspaceId}/work-report",
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-report",
            "a person of another account");
    }

    [Fact]
    public async Task BoardReport_ForASameAccountOutsider_AnswersLikeAMadeUpBoard()
    {
        await AssertRefusedIdenticallyAsync(
            _sameAccountOutsider,
            $"/api/v1/boards/{_a.BoardId}/report",
            $"/api/v1/boards/{Guid.NewGuid()}/report",
            "a person of the same account outside the Workspace");
    }

    [Fact]
    public async Task WorkspaceReport_ForASameAccountOutsider_AnswersLikeAMadeUpWorkspace()
    {
        await AssertRefusedIdenticallyAsync(
            _sameAccountOutsider,
            $"/api/v1/workspaces/{_a.WorkspaceId}/work-report",
            $"/api/v1/workspaces/{Guid.NewGuid()}/work-report",
            "a person of the same account outside the Workspace");
    }

    [Fact]
    public async Task ReportsOfAWorkspace_ForAUserWithNoMembershipAnywhere_AreRefused()
    {
        var board = await GetAsync(_unrelated, $"/api/v1/boards/{_a.BoardId}/report");
        var workspace = await GetAsync(_unrelated, $"/api/v1/workspaces/{_a.WorkspaceId}/work-report");

        board.Status.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        workspace.Status.Should().BeOneOf(new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
    }

    [Fact]
    public async Task OwnWorkspaceReport_IsStillReadable_ByItsOwner()
    {
        var own = await GetAsync(_a.User, $"/api/v1/workspaces/{_a.WorkspaceId}/work-report");

        own.Status.Should().Be(HttpStatusCode.OK, "the disclosure test is about outsiders, not about the route");
    }
}
