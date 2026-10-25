using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// Product events fan out to the Workspace's outgoing webhooks through the production delivery chain (worker
/// role, RLS enforced): an item created fact reaches each active connection once, a paused connection is
/// skipped, and a redelivery of the same fact adds no second delivery.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WebhookFanOutRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string FanOutEndpoint = "notrelix-integrations-webhook-fanout-item-created-v1";
    private const string DispatchEndpoint = "notrelix-integrations-webhook-dispatch-v1";

    private readonly PostgresTestContainer _db;

    public WebhookFanOutRuntimeChainIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnItemCreatedFactIsDeliveredOncePerActiveConnectionAndSkipsPausedOnes()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "Hook WS", $"hook-{Guid.NewGuid():N}", now);
        var active = IntegrationConnection.Create(accountId, workspace.Id, IntegrationProvider.Webhook, ownerId, now, targetUrl: "https://hooks.example.com/a");
        var paused = IntegrationConnection.Create(accountId, workspace.Id, IntegrationProvider.Webhook, ownerId, now, targetUrl: "https://hooks.example.com/b");
        paused.Pause(ownerId, now);

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.IntegrationConnections.Add(active);
            seed.IntegrationConnections.Add(paused);
            await seed.SaveChangesAsync();
        }

        var created = new BoardItemCreatedIntegrationEventV2(
            Guid.NewGuid(), accountId, Guid.NewGuid(), Guid.NewGuid(), workspace.Id, "Ship it", 1, Guid.NewGuid(),
            ActorUserId: ownerId, OccurredAt: DateTimeOffset.UtcNow);

        await using var provider = BuildProvider(ownerId);
        var hosted = provider.GetServices<IHostedService>().ToArray();
        foreach (var service in hosted) await service.StartAsync(CancellationToken.None);

        try
        {
            var bus = provider.GetRequiredService<IIntegrationEventBus>();
            await bus.PublishAsync(created);
            (await WaitForAsync(() => CountProcessedAsync(FanOutEndpoint), 1)).Should().BeTrue();
            (await WaitForAsync(() => CountProcessedAsync(DispatchEndpoint), 1)).Should().BeTrue("the active connection gets its delivery");
            await bus.PublishAsync(created);
            await Task.Delay(2000);
        }
        finally
        {
            foreach (var service in hosted.Reverse()) await service.StopAsync(CancellationToken.None);
        }

        (await CountProcessedAsync(DispatchEndpoint)).Should().Be(1, "one delivery per (event, active connection), also after a redelivery");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var connections = await probe.IntegrationConnections.IgnoreQueryFilters()
            .Where(c => c.WorkspaceId == workspace.Id).ToListAsync();
        connections.Single(c => c.Id == active.Id).LastErrorAt.Should().NotBeNull("the delivery reached the active connection");
        connections.Single(c => c.Id == paused.Id).LastErrorAt.Should().BeNull("a paused connection receives nothing");
    }

    private async Task<int> CountProcessedAsync(string consumerName)
    {
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        return await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
            .CountAsync(p => p.ConsumerName == consumerName && p.Status == "Succeeded");
    }

    private static async Task<bool> WaitForAsync(Func<Task<int>> count, int expected, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await count() >= expected) return true;
            await Task.Delay(200);
        }

        return false;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private ServiceProvider BuildProvider(Guid actorId)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["ConnectionStrings:NotrelixWorkerDb"] = _db.WorkerConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["Messaging:Transport"] = "InMemory",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
                ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "notrelix-test",
                ["JwtSettings:Audience"] = "notrelix-test",
                ["JwtSettings:ExpireMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpireDays"] = "7",
            })
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
            ["ConnectionStrings:NotrelixWorkerDb"] = _db.WorkerConnectionString,
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["Messaging:Transport"] = "InMemory",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
            ["JwtSettings:Issuer"] = "notrelix-test",
            ["JwtSettings:Audience"] = "notrelix-test",
            ["JwtSettings:ExpireMinutes"] = "30",
            ["JwtSettings:RefreshTokenExpireDays"] = "7",
        });

        builder.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        builder.Services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        builder.Services.AddSingleton(environment.Object);

        builder.Services.AddInfrastructure(configuration, environment.Object);

        builder.Services.AddScoped<ICurrentTenantContext>(_ => new CurrentTenantContext());
        builder.Services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser
        {
            UserId = actorId,
            Email = "chain-author@example.com",
            Name = "Chain Author",
        });

        builder.Services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        builder.AddApplicationServices();

        return builder.Services.BuildServiceProvider();
    }
}
