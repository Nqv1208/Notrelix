using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The owner/app connection split on the production host: migrate and policy application use
/// <c>ConnectionStrings:NotrelixMigrationDb</c>, serving uses <c>ConnectionStrings:NotrelixDb</c>, and
/// without an owner string both keep using the single runtime string.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class OwnerConnectionSplitIntegrationTests
{
    private readonly AppRoleTestContainer _fixture;

    public OwnerConnectionSplitIntegrationTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Host(string runtime, string? owner, string enforcement = "Off")
    {
        var extra = new Dictionary<string, string?> { ["Rls:RoleEnforcement"] = enforcement };
        if (owner is not null) extra["ConnectionStrings:NotrelixMigrationDb"] = owner;
        return AppRoleHost.CreateFactory(_fixture, runtime, extra);
    }

    private static string UserOf(string connectionString) => new NpgsqlConnectionStringBuilder(connectionString).Username!;

    [Fact]
    public async Task PolicyApplicationAndMigration_RunAsTheOwner_WhileServingRunsAsTheApplicationRole()
    {
        using var factory = Host(_fixture.AppConnectionString, _fixture.OwnerConnectionString);
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Database.GetDbConnection().ConnectionString.Should().Contain(UserOf(_fixture.AppConnectionString));
        scope.ServiceProvider.GetRequiredService<OwnerDbContextProvider>().UsesRuntimeFallback.Should().BeFalse();

        // Both would fail as notrelix_app (CREATE POLICY / schema changes need the owner).
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>().InitialiseAsync();
        await scope.ServiceProvider.GetRequiredService<RlsPolicyApplier>().ApplyAsync();
    }

    [Fact]
    public void WithoutAnOwnerString_BothConnectionsUseTheRuntimeString()
    {
        using var factory = Host(_fixture.OwnerConnectionString, owner: null);
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<OwnerDbContextProvider>();
        provider.UsesRuntimeFallback.Should().BeTrue();
        provider.Context.Database.GetDbConnection().ConnectionString
            .Should().Be(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void RoleEnforcementOff_StartsEvenOnTheSuperuser_AsTodayEveryEnvironmentDoes()
    {
        using var factory = Host(_fixture.OwnerConnectionString, owner: null, enforcement: "Off");
        factory.Services.Should().NotBeNull();
    }

    [Fact]
    public void RoleEnforcementEnforce_RefusesToStartOnTheSuperuser_AndStartsOnTheApplicationRole()
    {
        using var refused = Host(_fixture.OwnerConnectionString, owner: null, enforcement: "Enforce");
        FluentActions.Invoking(() => refused.CreateClient())
            .Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("is a superuser");

        using var accepted = Host(_fixture.AppConnectionString, _fixture.OwnerConnectionString, enforcement: "Enforce");
        using var client = accepted.CreateClient();
        client.Should().NotBeNull();
    }
}
