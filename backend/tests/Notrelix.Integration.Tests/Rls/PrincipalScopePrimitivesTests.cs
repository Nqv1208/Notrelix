using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Proves the bounded scope primitives against the policy pack under the application role: the helpers are
/// never satisfied by the wrong scope or the wrong identifiers, the entry function derives its bound from a
/// presented digest, the scope ends with the transaction, and no bounded scope is the worker bypass.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class PrincipalScopePrimitivesTests : IAsyncLifetime
{
    private static readonly SystemOperationReason Reason = new("InvitationToken", "scope primitive proof");
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public PrincipalScopePrimitivesTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-scope@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-scope@example.com", "Bbb", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<NpgsqlConnection> OpenAppAsync()
    {
        var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<string> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private static Task<string> EnterAsync(NpgsqlConnection c, NpgsqlTransaction tx, AppRoleTenant t) =>
        ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, @v)",
            ("h", t.InvitationTokenHash), ("v", t.InvitationHashVersion));

    [Fact]
    public async Task HasTokenScope_IsFalseWithoutAScope_AndInAppScopeEvenWhenTheValuesAreWritten()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();

        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
            .Should().Be("False");

        // The values alone do not make a token scope: the scope kind must be 'token'.
        await ScalarAsync(c, tx,
            "SELECT set_config('app.scope_account_id', @a, true), set_config('app.scope_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            ("a", _a.AccountId.ToString()), ("w", _a.WorkspaceId.ToString()));
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
            .Should().Be("False");
    }

    [Fact]
    public async Task EnterTokenScope_BindsTheTokensOwnAccountAndWorkspace_AndNothingElse()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();

        (await EnterAsync(c, tx, _a)).Should().Be("1");

        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("True");
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, NULL)", ("a", _a.AccountId))).Should().Be("True");
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _b.AccountId), ("w", _b.WorkspaceId))).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _b.WorkspaceId))).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(NULL, NULL)")).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_user_scope(@u)", ("u", _a.User.Id))).Should().Be("False", "a token scope carries no user");
    }

    [Fact]
    public async Task TokenScope_IsNotTheWorkerBypass_AndWidensNoExistingPolicy()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();
        (await EnterAsync(c, tx, _a)).Should().Be("1");

        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_workspace_access(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
            .Should().Be("False", "the grant-based helpers still need a user");
        foreach (var table in new[] { "identity.users", "account.accounts", "workspace.workspaces", "workspace.workspace_invitations", "work.boards", "authz.access_grants" })
        {
            (await ScalarAsync(c, tx, $"SELECT count(*) FROM {table}")).Should().Be("0", $"{table} must stay invisible to a bare token scope");
        }
    }

    [Fact]
    public async Task EnterTokenScope_WithAnUnknownDigest_EntersNothing()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();

        (await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', 'no-such-digest', 1)")).Should().Be("0");
        (await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, 99)", ("h", _a.InvitationTokenHash))).Should().Be("0", "the hash version is part of the identity");
        (await ScalarAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("app");
        (await ScalarAsync(c, tx, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("False");
    }

    [Fact]
    public async Task EnterTokenScope_RejectsAnUnknownPurpose()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();

        var act = () => ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('anything-else', @h, 1)", ("h", _a.InvitationTokenHash));
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("22023");
    }

    [Fact]
    public async Task TokenScope_EndsWithTheTransaction_SoAPooledConnectionStartsClean()
    {
        await using var c = await OpenAppAsync();
        await using (var tx = await c.BeginTransactionAsync())
        {
            (await EnterAsync(c, tx, _a)).Should().Be("1");
            await tx.CommitAsync();
        }

        await using var next = await c.BeginTransactionAsync();
        (await ScalarAsync(c, next, "SELECT ops.current_request_scope()")).Should().Be("app");
        (await ScalarAsync(c, next, "SELECT ops.has_token_scope(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("False");
        (await ScalarAsync(c, next, "SELECT coalesce(nullif(current_setting('app.scope_account_id', true), ''), '-')")).Should().Be("-");
    }

    [Fact]
    public async Task HasUserScope_MatchesOnlyTheUserTheScopeWasEnteredFor()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();
        await ScalarAsync(c, tx,
            "SELECT set_config('app.request_scope', 'user', true), set_config('app.scope_user_id', @u, true)",
            ("u", _a.User.Id.ToString()));

        (await ScalarAsync(c, tx, "SELECT ops.has_user_scope(@u)", ("u", _a.User.Id))).Should().Be("True");
        (await ScalarAsync(c, tx, "SELECT ops.has_user_scope(@u)", ("u", _b.User.Id))).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.has_user_scope(NULL)")).Should().Be("False");
        (await ScalarAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False");

        await ScalarAsync(c, tx, "SELECT set_config('app.request_scope', 'app', true)");
        (await ScalarAsync(c, tx, "SELECT ops.has_user_scope(@u)", ("u", _a.User.Id))).Should().Be("False", "the user scope kind is part of the check");
    }

    [Fact]
    public async Task EntryFunction_IsExecutableByTheApplicationRoleOnly()
    {
        await using var c = await OpenAppAsync();
        await using var tx = await c.BeginTransactionAsync();
        const string signature = "ops.enter_token_scope(text,text,integer)";

        (await ScalarAsync(c, tx, $"SELECT has_function_privilege('notrelix_app', '{signature}', 'EXECUTE')")).Should().Be("True");
        foreach (var role in new[] { "notrelix_worker", "notrelix_auth", "notrelix_support_readonly", "public" })
        {
            (await ScalarAsync(c, tx, $"SELECT has_function_privilege('{role}', '{signature}', 'EXECUTE')"))
                .Should().Be("False", $"{role} must not enter token scopes");
        }
    }

    [Fact]
    public async Task RlsSessionContext_AppliesTheUserScope_AndRequiresATransaction()
    {
        await using var db = _fixture.CreateAppContext(new FakeCurrentTenantContext());
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), new FakeCurrentTenantContext());

        var outside = () => rls.ApplyUserScopeAsync(_a.User.Id, default);
        await outside.Should().ThrowAsync<InvalidOperationException>();

        await using var tx = await db.Database.BeginTransactionAsync();
        await rls.ApplyUserScopeAsync(_a.User.Id, default);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var npgsqlTx = (NpgsqlTransaction)tx.GetDbTransaction();
        (await ScalarAsync(connection, npgsqlTx, "SELECT ops.current_request_scope()")).Should().Be("user");
        (await ScalarAsync(connection, npgsqlTx, "SELECT ops.current_user_id()::text")).Should().Be(_a.User.Id.ToString());
        (await ScalarAsync(connection, npgsqlTx, "SELECT ops.has_user_scope(@u)", ("u", _a.User.Id))).Should().Be("True");
        (await ScalarAsync(connection, npgsqlTx, "SELECT ops.is_worker_scope()")).Should().Be("False");
    }

    [Fact]
    public async Task RlsSessionContext_EntersTheTokenScope_AndReportsTheDerivedBinding()
    {
        await using var db = _fixture.CreateAppContext(new FakeCurrentTenantContext());
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), new FakeCurrentTenantContext());
        var request = new TokenScopeRequest(TokenScopePurpose.WorkspaceInvitation, _a.InvitationTokenHash, _a.InvitationHashVersion, Reason);

        var outside = () => rls.EnterTokenScopeAsync(request, default);
        await outside.Should().ThrowAsync<InvalidOperationException>();

        await using var tx = await db.Database.BeginTransactionAsync();
        var binding = await rls.EnterTokenScopeAsync(request, default);
        binding.Should().Be(new TokenScopeBinding(_a.AccountId, _a.WorkspaceId));

        var miss = await rls.EnterTokenScopeAsync(request with { TokenHash = "unknown" }, default);
        miss.Should().BeNull();
    }
}
