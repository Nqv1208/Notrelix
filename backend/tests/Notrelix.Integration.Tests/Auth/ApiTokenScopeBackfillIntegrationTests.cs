using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Identity;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// A database at the schema before scopes were enforced is upgraded through the real migration chain: a token that
/// carried no scope list is granted every scope (so it behaves as it did and nothing is lost), a token that already
/// holds a list, even an empty one, is left alone, no row is lost, and running the backfill again changes nothing.
/// Uses a dedicated container because the shared one is already at the latest schema.
/// </summary>
public sealed class ApiTokenScopeBackfillIntegrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261119180157_AddChatMessageSticker";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_api_token_scopes")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBefore);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Migration_GrantsEveryScopeToATokenWithNone_LeavesTheOthers_AndARerunChangesNothing()
    {
        var legacy = await InsertTokenAsync("legacy", scopes: null);
        var legacyRevoked = await InsertTokenAsync("legacy revoked", scopes: null, status: "Revoked");
        var narrow = await InsertTokenAsync("narrow", scopes: """["docs:read"]""");
        var empty = await InsertTokenAsync("empty", scopes: "[]");

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        (await ScopesOfAsync(legacy)).Should().Be(ApiTokenScopeMigrationSql.EveryScopeJson);
        (await ScopesOfAsync(legacyRevoked)).Should().Be(ApiTokenScopeMigrationSql.EveryScopeJson, "a revoked token is migrated too: none is left without scopes");
        (await ScopesOfAsync(narrow)).Should().Be("""["docs:read"]""", "a list already chosen is never widened");
        (await ScopesOfAsync(empty)).Should().Be("[]", "an empty list means no access and stays so");
        (await QueryAsync("SELECT count(*)::text FROM identity.api_tokens")).Single().Should().Be("4", "no token is lost");

        // The migrated text is what the application reads: every scope of the vocabulary, each spelled as the domain does.
        Notrelix.Domain.Identity.Tokens.ApiTokenScopes.FromJson(await ScopesOfAsync(legacy)).Scopes
            .Should().BeEquivalentTo(Notrelix.Domain.Identity.Tokens.ApiTokenScopes.Vocabulary);

        var before = await QueryAsync("SELECT id::text || '=' || coalesce(scopes, '<null>') FROM identity.api_tokens ORDER BY id");
        var affected = await ExecuteAsync(ApiTokenScopeMigrationSql.Backfill);

        affected.Should().Be(0, "no token is without scopes any more");
        (await QueryAsync("SELECT id::text || '=' || coalesce(scopes, '<null>') FROM identity.api_tokens ORDER BY id")).Should().Equal(before);
    }

    private async Task<Guid> InsertTokenAsync(string name, string? scopes, string status = "Active")
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.api_tokens (id, account_id, workspace_id, user_id, name, token_hash, scopes, status, created_at, version)
            VALUES (@id, @account, @workspace, @user, @name, @hash, @scopes, @status, now(), 1)
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("account", Guid.NewGuid());
        command.Parameters.AddWithValue("workspace", Guid.NewGuid());
        command.Parameters.AddWithValue("user", Guid.NewGuid());
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("hash", $"hash-{id:N}");
        command.Parameters.AddWithValue("scopes", (object?)scopes ?? DBNull.Value);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private async Task<string> ScopesOfAsync(Guid id) =>
        (await QueryAsync($"SELECT coalesce(scopes, '<null>') FROM identity.api_tokens WHERE id = '{id}'")).Single();

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;

        return new ApplicationDbContext(options);
    }

    private async Task<int> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private async Task<List<string>> QueryAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }
}
