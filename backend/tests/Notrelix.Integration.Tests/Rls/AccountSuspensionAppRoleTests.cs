using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

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

    private async Task<Guid> QueueImportAsync()
    {
        var jobId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO ops.import_jobs (id, workspace_id, job_type, target_resource_type, target_resource_id, status, " +
            "total_records, processed_records, succeeded_records, failed_records, options_json, requested_by_user_id, created_at) " +
            "VALUES (@id, @workspace, 'board-import', 'Board', @board, 'queued', 1, 0, 0, 0, '{}'::jsonb, @user, now())", connection);
        command.Parameters.AddWithValue("id", jobId);
        command.Parameters.AddWithValue("workspace", _a.WorkspaceId);
        command.Parameters.AddWithValue("board", _a.BoardId);
        command.Parameters.AddWithValue("user", _a.User.Id);
        await command.ExecuteNonQueryAsync();
        return jobId;
    }

    private async Task<(string Status, string Result)> ImportJobAsync(Guid jobId)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT status, coalesce(result_json::text, '') FROM ops.import_jobs WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", jobId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return (reader.GetString(0), reader.GetString(1));
    }

    /// <summary>
    /// The host runs its own transfer loop, which claims the queued job within a couple of seconds; if nothing has
    /// claimed it after a short wait the runner is driven by hand, in a worker scope like the loop's. Either way the
    /// same runner code finishes the job, and this returns its final state.
    /// </summary>
    private async Task<(string Status, string Result)> FinishedImportAsync(Guid jobId)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var job = await ImportJobAsync(jobId);
            if (job.Status is "failed" or "succeeded")
                return job;
            if (job.Status == "queued" && attempt >= 6)
            {
                await using var scope = Factory.Services.GetRequiredService<IServiceScopeFactory>().CreateWorkerAsyncScope();
                await scope.ServiceProvider.GetRequiredService<BoardTransferRunner>().RunNextAsync(CancellationToken.None);
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"The import job {jobId} did not finish.");
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Closed")]
    public async Task AnImportQueuedBeforeTheSuspension_IsRefusedAndCreatesNothing_ThenRunsOnceLifted(string status)
    {
        // Suspended first, queued second: the host's own loop may claim the job the moment it exists.
        await SetStatusAsync(status);
        var jobId = await QueueImportAsync();

        var refused = await FinishedImportAsync(jobId);
        refused.Status.Should().Be("failed");
        refused.Result.Should().Contain("account-suspended", "a suspended Account is read-only for background work too");

        // Lifted: the same job is picked up and no longer refused for the Account (this one has no file, so it
        // fails for that reason instead, which proves the suspension check is what stopped the first run).
        await SetStatusAsync("Active");
        await using (var connection = new NpgsqlConnection(_fixture.OwnerConnectionString))
        {
            await connection.OpenAsync();
            await using var requeue = new NpgsqlCommand("UPDATE ops.import_jobs SET status = 'queued', result_json = NULL WHERE id = @id", connection);
            requeue.Parameters.AddWithValue("id", jobId);
            await requeue.ExecuteNonQueryAsync();
        }

        (await FinishedImportAsync(jobId)).Result.Should().NotContain("account-suspended");
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
