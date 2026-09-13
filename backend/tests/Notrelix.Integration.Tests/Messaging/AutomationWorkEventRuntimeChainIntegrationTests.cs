using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-AI-001/002 — production transport proof for the runtime-reachable
/// Work triggers: a published board_item.moved / board.item.created fact
/// travels TenantContextConsumeFilter → DeduplicationConsumeFilter → the
/// real thin Automation consumer → the evaluator, creating exactly one
/// durable execution + staged intent. The moved chain additionally
/// redelivers the exact same business EventId and observes the SECOND real
/// claim attempt reaching the dedup filter before asserting single-execution —
/// never a sleep.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationWorkEventRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string MovedConsumerEndpoint = "notrelix-automation-board-item-moved-v1";
    private const string CreatedConsumerEndpoint = "notrelix-automation-board-item-created-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationWorkEventRuntimeChainIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record ChainGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid OwnerId,
        Guid RuleId,
        Guid ItemId,
        Guid BoardId,
        Guid GroupId);

    private async Task<ChainGraph> SeedActiveRuleAsync(string triggerType, string triggerConfig)
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "Auto Chain WS", $"ac-{Guid.NewGuid():N}", now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Chain Board", null, now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, now);

        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create(triggerType, triggerConfig.Replace("{group}", group.Id.ToString())),
            AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-chain"}"""));
        var rule = AutomationRule.Create(accountId, workspace.Id, "Chain rule", config, ownerId, now);
        rule.Enable(ownerId, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        seed.AutomationRules.Add(rule);
        await seed.SaveChangesAsync();

        return new ChainGraph(accountId, workspace.Id, ownerId, rule.Id, item.Id, board.Id, group.Id);
    }

    [Fact]
    public async Task MovedWorkEvent_ReachesRealConsumer_AndRedeliveryClaimsExactlyOneExecution()
    {
        var graph = await SeedActiveRuleAsync("ItemMovedToGroup", """{"groupId":"{group}"}""");
        await using var provider = BuildProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var eventId = Guid.CreateVersion7();
            var first = NewMovedEvent(graph, eventId);
            await provider.GetRequiredService<IIntegrationEventBus>().PublishAsync(first);

            var firstDelivered = await WaitForAsync(async () =>
                await ExecutionCountAsync(graph.RuleId) == 1);
            firstDelivered.Should().BeTrue(await DiagnosticAsync(MovedConsumerEndpoint));

            // Redeliver the SAME business event: the proof observes the
            // second claim attempt actually reaching the dedup filter.
            await provider.GetRequiredService<IIntegrationEventBus>().PublishAsync(first);
            (await WaitForAsync(async () =>
                RecordingDeduplicationStore.ClaimAttempts(eventId, MovedConsumerEndpoint) >= 2)).Should().BeTrue(
                "the duplicate delivery must actually reach the DeduplicationConsumeFilter");

            // Give the loser of the claim time to skip; the single INSERT is
            // the durable authority on execution identity.
            await Task.Delay(500);
            (await ExecutionCountAsync(graph.RuleId)).Should().Be(1,
                "a redelivered Work event must never create a second execution");
            (await IntentCountAsync(graph.WorkspaceId, "automation.n8n-dispatch-requested")).Should().Be(1,
                "exactly one durable dispatch intent exists for the single execution");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task CreatedWorkEvent_ReachesRealConsumer_AndStagesExecutionIntent()
    {
        var graph = await SeedActiveRuleAsync("ItemCreated", """{"boardId":"{group}"}""");
        await using var provider = BuildProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var createdEventId = Guid.CreateVersion7();
            await provider.GetRequiredService<IIntegrationEventBus>()
                .PublishAsync(NewCreatedEvent(graph, createdEventId));

            var delivered = await WaitForAsync(async () =>
                await ExecutionCountAsync(graph.RuleId) == 1);
            if (!delivered)
            {
                delivered.Should().BeTrue(await DiagnosticAsync(CreatedConsumerEndpoint));
            }
            (await IntentCountAsync(graph.WorkspaceId, "automation.n8n-dispatch-requested")).Should().Be(1);
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }
    }

    private static BoardItemMovedIntegrationEvent NewMovedEvent(ChainGraph graph, Guid eventId) =>
        new(eventId, graph.AccountId, graph.ItemId, graph.BoardId, graph.WorkspaceId,
            graph.GroupId, graph.GroupId, Guid.NewGuid(), ActorUserId: graph.OwnerId,
            OccurredAt: DateTimeOffset.UtcNow);

    private static BoardItemCreatedIntegrationEvent NewCreatedEvent(ChainGraph graph, Guid eventId) =>
        new(eventId, graph.AccountId, graph.ItemId, graph.BoardId, graph.WorkspaceId,
            "Chained item", Guid.NewGuid(), ActorUserId: graph.OwnerId,
            OccurredAt: DateTimeOffset.UtcNow);

    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
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

        // Full production composition — the real consumer, the tenant
        // restoration filter, and the dedup filter are exactly the shipped
        // graph; only the dedup store gains a recording decorator.
        builder.Services.AddInfrastructure(configuration, environment.Object);
        builder.AddApplicationServices();

        builder.Services.Replace(
            ServiceDescriptor.Scoped<IMessageDeduplicationStore>(sp =>
                new RecordingDeduplicationStore(
                    new MessageDeduplicationStore(
                        sp.GetRequiredService<ApplicationDbContext>(),
                        sp.GetRequiredService<Notrelix.Application.Common.Time.IDateTimeProvider>(),
                        sp.GetRequiredService<MetricsService>()))));

        return builder.Services.BuildServiceProvider();
    }

    private async Task<int> ExecutionCountAsync(Guid ruleId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters()
            .CountAsync(e => e.RuleId == ruleId);
    }

    private async Task<int> IntentCountAsync(Guid workspaceId, string messageName)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(m => m.WorkspaceId == workspaceId && m.MessageName == messageName);
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }

    private async Task<string> DiagnosticAsync(string consumerEndpoint)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        var processed = await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
            .Where(p => p.ConsumerName == consumerEndpoint)
            .Select(p => p.EventId + ":" + p.Status)
            .ToListAsync();
        var executions = await probe.AutomationExecutions.IgnoreQueryFilters()
            .Select(e => e.RuleId + ":" + e.Status)
            .ToListAsync();
        var claims = RecordingDeduplicationStore.AllAttemptsSnapshot();
        return $"processed=[{string.Join(",", processed)}] executions=[{string.Join(",", executions)}] claims=[{claims}]";
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
