using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.SharedKernel;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Reading one connection in detail (UC-INT-B07): a person outside the Workspace gets the same refusal for a connection
/// that exists there and for an id that never existed. The two statuses are compared to each other and must be a
/// refusal (403 or 404); the bodies are not compared, because each answer carries its own correlation id. Proven over
/// the real production graph on the application role.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class IntegrationConnectionDetailNonMemberTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _stranger = null!;
    private Guid _connectionId;

    public IntegrationConnectionDetailNonMemberTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "b07-owner@example.com", "Own", "hash");
        _stranger = await AppRoleUsers.AddAsync(_fixture, "b07-stranger@example.com", "Str", "hash");

        var connection = IntegrationConnection.Create(
            _a.AccountId, _a.WorkspaceId, IntegrationProvider.Webhook, _a.User.Id, Now, targetUrl: "https://hooks.example.test/b07");
        connection.RotateSecret("1", SecretRef.Create("opaque-b07-reference"), _a.User.Id, Now);
        _connectionId = connection.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.IntegrationConnections.Add(connection);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpStatusCode> GetAsync(User who, Guid workspaceId, Guid connectionId, WebApplicationFactory<Program> factory)
    {
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/integrations/{connectionId}");
        return response.StatusCode;
    }

    [Fact]
    public async Task Stranger_GetsTheSameRefusal_ForAnExistingConnectionAndAMadeUpId()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

        var found = await GetAsync(_stranger, _a.WorkspaceId, _connectionId, factory);
        var made = await GetAsync(_stranger, _a.WorkspaceId, Guid.NewGuid(), factory);

        new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound }.Should().Contain(made);
        found.Should().Be(made, "a connection the stranger cannot see is answered like one that does not exist");
    }

    [Fact]
    public async Task Owner_ReadsTheConnection()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

        var status = await GetAsync(_a.User, _a.WorkspaceId, _connectionId, factory);

        status.Should().Be(HttpStatusCode.OK);
    }
}
