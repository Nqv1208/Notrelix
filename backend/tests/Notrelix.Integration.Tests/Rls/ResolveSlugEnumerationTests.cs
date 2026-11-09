using System.Net;
using System.Text.Json;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-WSP-B02 through the real anonymous route under the application role (WGREQ032): a live address answers
/// its card, and a made-up address and a deleted workspace's address get the same refusal, so the answer does
/// not show which addresses exist. The status is compared with the other refusal, never hard-coded.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ResolveSlugEnumerationTests : IAsyncLifetime
{
    private static readonly string[] VolatileKeys = ["correlationId", "traceId", "instance"];
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;

    public ResolveSlugEnumerationTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "enum-resolve@example.com", "Enum", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ResolveSlug_ALiveAddressAnswersItsCard_AndADeletedOrMadeUpAddressAnswersTheSameRefusal()
    {
        var slug = await OwnerScalarAsync("SELECT slug FROM workspace.workspaces WHERE id = @w", ("w", _a.WorkspaceId));

        var live = await CallAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug={slug}");
        live.StatusCode.Should().Be(HttpStatusCode.OK, await live.Content.ReadAsStringAsync());
        (await live.Content.ReadAsStringAsync()).Should().Contain(_a.WorkspaceId.ToString());

        await OwnerScalarAsync("UPDATE workspace.workspaces SET deleted_at = now() WHERE id = @w", ("w", _a.WorkspaceId));

        var deleted = await CallAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug={slug}");
        var madeUp = await CallAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug=nobody-{Guid.NewGuid():N}");

        deleted.StatusCode.Should().Be(madeUp.StatusCode, "a deleted address and an unknown one get the same answer");
        ((int)madeUp.StatusCode).Should().BeInRange(400, 499, "the refusal is a client error, not a card");
        (await CanonicalAsync(deleted)).Should().BeEquivalentTo(await CanonicalAsync(madeUp));
    }

    private static async Task<Dictionary<string, string>> CanonicalAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateObject()
            .Where(property => !VolatileKeys.Contains(property.Name))
            .ToDictionary(property => property.Name, property => property.Value.GetRawText());
    }

    private async Task<HttpResponseMessage> CallAsync(string path)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();
        var response = await client.GetAsync(path);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }
}
