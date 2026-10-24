using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.API;
using Notrelix.Application.Features.Identity.Auth.Sessions;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Negative proofs for the <c>user</c> scope under the application role: a user reaches only the rows the
/// user's own grants and identity name, never another tenant's, through Global requests, the resource
/// locator and direct statements in the scope.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class UserScopeIsolationTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public UserScopeIsolationTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var f = CreateFactory(_fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword("Correct-Horse-9!");

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-user-scope@example.com", "Aaa", hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-user-scope@example.com", "Bbb", hash);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> CreateFactory(string db) => AppRoleHost.CreateFactory(_fixture, db);

    private async Task<HttpResponseMessage> AsAsync(AppRoleTenant who, Func<HttpClient, Task<HttpResponseMessage>> call)
    {
        using var factory = CreateFactory(_fixture.AppConnectionString);
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who.User);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await call(client);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    [Fact]
    public async Task ResourceLocator_HidesAnotherTenantsBoard_AsNotFound_AndStillFindsTheCallersOwn()
    {
        (await AsAsync(_a, c => c.GetAsync($"/api/v1/boards/{_a.BoardId}"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await AsAsync(_b, c => c.GetAsync($"/api/v1/boards/{_b.BoardId}"))).StatusCode.Should().Be(HttpStatusCode.OK);

        (await AsAsync(_b, c => c.GetAsync($"/api/v1/boards/{_a.BoardId}"))).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "B holds no grant on A's workspace, so the locator cannot see the row");
        (await AsAsync(_a, c => c.GetAsync($"/api/v1/boards/{_b.BoardId}"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AsAsync(_a, c => c.GetAsync($"/api/v1/boards/{Guid.NewGuid()}"))).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "an unknown id and a hidden id are indistinguishable");
    }

    [Fact]
    public async Task GlobalRequests_ReturnOnlyTheCallersOwnData()
    {
        var workspacesOfA = await (await AsAsync(_a, c => c.GetAsync("/api/v1/account/workspaces"))).Content.ReadAsStringAsync();
        var workspacesOfB = await (await AsAsync(_b, c => c.GetAsync("/api/v1/account/workspaces"))).Content.ReadAsStringAsync();
        workspacesOfA.Should().Contain("Aaa WS").And.NotContain("Bbb WS");
        workspacesOfB.Should().Contain("Bbb WS").And.NotContain("Aaa WS");

        var meOfB = await (await AsAsync(_b, c => c.GetAsync("/api/v1/auth/me"))).Content.ReadAsStringAsync();
        meOfB.Should().Contain("b-user-scope@example.com").And.NotContain("a-user-scope@example.com");

        var bootstrapOfB = await (await AsAsync(_b, c => c.GetAsync("/api/v1/auth/bootstrap"))).Content.ReadAsStringAsync();
        bootstrapOfB.Should().NotContain("a-user-scope@example.com").And.NotContain("Aaa WS");
    }

    [Fact]
    public async Task ProfileUpdate_ByOneUser_NeverChangesAnotherUsersRow()
    {
        (await AsAsync(_b, c => c.PatchAsJsonAsync("/api/v1/profile/", new { name = "Bbb Renamed" }))).StatusCode
            .Should().Be(HttpStatusCode.OK);

        (await AsAsync(_a, c => c.GetAsync("/api/v1/auth/me"))).Content.ReadAsStringAsync().Result
            .Should().Contain("Aaa").And.NotContain("Bbb Renamed");
        (await AsAsync(_b, c => c.GetAsync("/api/v1/auth/me"))).Content.ReadAsStringAsync().Result
            .Should().Contain("Bbb Renamed");
    }

    private async Task<T> InUserScopeAsync<T>(AppRoleTenant who, Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> body)
    {
        await using var db = _fixture.CreateAppContext(new FakeCurrentTenantContext());
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), new FakeCurrentTenantContext());
        await using var tx = await db.Database.BeginTransactionAsync();
        await rls.ApplyUserScopeAsync(who.User.Id, default);
        return await body((NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction());
    }

    private static async Task<long> CountAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    [Theory]
    [InlineData("identity.users")]
    [InlineData("account.accounts")]
    [InlineData("workspace.workspaces")]
    [InlineData("workspace.workspace_members")]
    [InlineData("workspace.workspace_invitations")]
    [InlineData("work.boards")]
    [InlineData("authz.access_grants")]
    public async Task UserScope_SeesOnlyTheCallersRowsInEveryTenantTable(string table)
    {
        var (own, total) = await InUserScopeAsync(_b, async (c, tx) =>
        (
            await CountAsync(c, tx, $"SELECT count(*) FROM {table} WHERE {OwnerColumn(table)} = @id", ("id", OwnerKey(table, _b))),
            await CountAsync(c, tx, $"SELECT count(*) FROM {table}")
        ));

        own.Should().Be(1, $"B sees B's own row in {table}");
        total.Should().Be(1, $"B sees nothing of A in {table}");
    }

    private static string OwnerColumn(string table) => table switch
    {
        "identity.users" => "id",
        "account.accounts" => "id",
        "workspace.workspaces" => "id",
        "work.boards" => "id",
        "authz.access_grants" => "user_id",
        "workspace.workspace_members" => "user_id",
        _ => "workspace_id",
    };

    private static Guid OwnerKey(string table, AppRoleTenant t) => table switch
    {
        "identity.users" or "authz.access_grants" or "workspace.workspace_members" => t.User.Id,
        "account.accounts" => t.AccountId,
        "work.boards" => t.BoardId,
        _ => t.WorkspaceId,
    };

    [Fact]
    public async Task UserScope_CannotWriteAnotherTenantsRows()
    {
        var updated = await InUserScopeAsync(_b, async (c, tx) =>
        {
            await using var upd = new NpgsqlCommand("UPDATE identity.users SET name = 'hijacked' WHERE id = @id", c, tx);
            upd.Parameters.AddWithValue("id", _a.User.Id);
            return await upd.ExecuteNonQueryAsync();
        });
        updated.Should().Be(0);

        await using var db = _fixture.CreateAppContext(new FakeCurrentTenantContext());
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), new FakeCurrentTenantContext());
        await using var tx = await db.Database.BeginTransactionAsync();
        await rls.ApplyUserScopeAsync(_b.User.Id, default);
        db.Boards.Add(Notrelix.Domain.WorkManagement.Boards.Board.Create(
            _a.AccountId, _a.WorkspaceId, _b.User.Id, "planted", null, DateTimeOffset.UtcNow));

        var act = () => db.SaveChangesAsync();
        var failure = await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
        failure.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be("42501", "row-level security rejects the write into A's workspace");
    }

    [Fact]
    public async Task UserScope_OfAUserWithoutGrants_SeesNoTenantRows_AndEndsWithTheTransaction()
    {
        var stranger = new AppRoleTenant(
            Notrelix.Domain.Identity.Users.User.Create("stranger@example.com", "Stranger", "hash", DateTimeOffset.UtcNow, hasPasswordCredential: true),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "", 1);
        var visible = await InUserScopeAsync(stranger, async (c, tx) =>
            await CountAsync(c, tx, "SELECT (SELECT count(*) FROM work.boards) + (SELECT count(*) FROM workspace.workspaces) + (SELECT count(*) FROM identity.users)"));
        visible.Should().Be(0);

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        (await CountAsync(connection, tx, "SELECT count(*) FROM identity.users")).Should().Be(0, "the scope did not outlive its transaction");
    }
}
