using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Integrations.N8n.Providers;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-PF-FLOW-04 (M11) — production-composition proof for the n8n dispatch
/// chain: N8nDispatchRequestedV1 travels TenantContextConsumeFilter →
/// DeduplicationConsumeFilter (command-owned path) → the real
/// N8nDispatchConsumer with its own transaction + RLS → the provider adapter
/// port. Exactly the shipped graph; only the provider port and the dedup store
/// are replaced with fakes. Proves: (1) an unknown provider outcome settles the
/// execution as Failed with a reconciliation marker and never auto re-fires,
/// and (2) a retryable outcome persists durable attempt evidence across real
/// MassTransit retries (the first attempt's attempt-count survives before the
/// retry re-runs the consumer).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class N8nDispatchRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string N8nDispatchEndpoint = "notrelix-automation-n8n-dispatch-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public N8nDispatchRuntimeChainIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
        RecordingDeduplicationStore.Reset();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record ChainGraph(Guid AccountId, Guid WorkspaceId, Guid RuleId, Guid ExecutionId);

    private async Task<ChainGraph> SeedRuleAndExecutionAsync()
    {
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"n8n-runtime-{Guid.NewGuid():N}@example.com", "Runtime N8N", "hashed", now, true);
        var account = Notrelix.Domain.Accounts.Accounts.Account.Create(
            "Runtime N8N Account", $"runtime-{Guid.NewGuid():N}",
            Notrelix.Domain.Accounts.Accounts.AccountType.Team, ownerId, now);
        var workspace = Workspace.Create(account.Id, ownerId, "Runtime WS", $"runtime-{Guid.NewGuid():N}", now);

        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemAssigned"),
            AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}"""));
        var rule = AutomationRule.Create(account.Id, workspace.Id, "Runtime alert", config, ownerId, now);
        rule.Enable(ownerId, now);
        var execution = AutomationExecution.Create(account.Id, workspace.Id, rule.Id, Guid.NewGuid(), now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(Notrelix.Domain.Workspaces.Members.WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, Notrelix.Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now));
        seed.AutomationRules.Add(rule);
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();

        return new ChainGraph(account.Id, workspace.Id, rule.Id, execution.Id);
    }

    [Fact]
    public async Task UnknownOutcome_SettlesFailedWithReconciliation_AndNeverReFires()
    {
        var graph = await SeedRuleAndExecutionAsync();
        var providerN8n = new RecordingN8nClient();
        providerN8n.Queue(N8nWebhookOutcome.UnknownOutcome, "n8n webhook call timed out");

        await using var provider = BuildProvider(providerN8n);
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var message = NewDispatchMessage(graph);
            await provider.GetRequiredService<IIntegrationEventBus>().PublishAsync(message);

            var settled = await WaitForAsync(async () =>
                await ExecutionStatusAsync(graph.ExecutionId) == AutomationExecutionStatus.Failed);

            if (!settled)
            {
                settled.Should().BeTrue(await DiagnosticAsync(graph));
            }

            var stored = await LoadExecutionAsync(graph.ExecutionId);
            stored.Should().NotBeNull();
            stored!.Status.Should().Be(AutomationExecutionStatus.Failed);
            stored.Error.Should().Contain("reconciliation required");

            providerN8n.CallCount.Should().Be(1,
                "an unknown outcome must never auto re-fire the provider call");
            RecordingDeduplicationStore.ClaimAttempts(message.EventId, N8nDispatchEndpoint).Should().Be(1,
                "the unknown outcome settles in one delivery; no redelivery is requested");
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
    public async Task RetryableRateLimit_DurableAttemptEvidence_SurvivesRealMassTransitRetry()
    {
        var graph = await SeedRuleAndExecutionAsync();
        var providerN8n = new RecordingN8nClient();
        providerN8n.Queue(N8nWebhookOutcome.RetryableFailure, "n8n returned HTTP 429: busy");
        providerN8n.Queue(N8nWebhookOutcome.Succeeded);

        await using var provider = BuildProvider(providerN8n);
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var message = NewDispatchMessage(graph);
            await provider.GetRequiredService<IIntegrationEventBus>().PublishAsync(message);

            var settled = await WaitForAsync(async () =>
                await ExecutionStatusAsync(graph.ExecutionId) == AutomationExecutionStatus.Succeeded);

            if (!settled)
            {
                settled.Should().BeTrue(await DiagnosticAsync(graph));
            }

            providerN8n.CallCount.Should().BeGreaterThanOrEqualTo(2,
                "MassTransit must redeliver the retryable outcome (real retry exercised)");

            var stored = await LoadExecutionAsync(graph.ExecutionId);
            stored.Should().NotBeNull();
            stored!.Status.Should().Be(AutomationExecutionStatus.Succeeded);
            stored.AttemptCount.Should().Be(1,
                "the first retryable attempt's evidence is durable and survived the real retry");

            RecordingDeduplicationStore.ClaimAttempts(message.EventId, N8nDispatchEndpoint).Should().BeGreaterThanOrEqualTo(2,
                "each real delivery re-claims under the stable execution identity");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }
    }

    private static N8nDispatchRequestedV1 NewDispatchMessage(ChainGraph graph) =>
        new(Guid.CreateVersion7(), graph.ExecutionId, graph.RuleId,
            graph.AccountId, graph.WorkspaceId, DateTimeOffset.UtcNow,
            Guid.NewGuid(), SourceEventId: null, CausationId: null);

    private ServiceProvider BuildProvider(IN8nClient providerN8n)
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
                ["N8n:Enabled"] = "false",
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
            ["N8n:Enabled"] = "false",
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

        // Full production composition — real consumer, tenant restoration filter,
        // and dedup filter; only the provider port and dedup store are faked.
        builder.Services.AddInfrastructure(configuration, environment.Object);
        builder.AddApplicationServices();

        builder.Services.Replace(ServiceDescriptor.Singleton<IN8nClient>(providerN8n));

        builder.Services.Replace(
            ServiceDescriptor.Scoped<IMessageDeduplicationStore>(sp =>
                new RecordingDeduplicationStore(
                    new MessageDeduplicationStore(
                        sp.GetRequiredService<ApplicationDbContext>(),
                        sp.GetRequiredService<Notrelix.Application.Common.Time.IDateTimeProvider>(),
                        sp.GetRequiredService<MetricsService>()))));

        return builder.Services.BuildServiceProvider();
    }

    private async Task<AutomationExecution?> LoadExecutionAsync(Guid id)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task<AutomationExecutionStatus?> ExecutionStatusAsync(Guid id)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .Select(x => (AutomationExecutionStatus?)x.Status)
            .FirstOrDefaultAsync();
    }

    private async Task<string> DiagnosticAsync(ChainGraph graph)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        var processed = await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
            .Where(p => p.ConsumerName == N8nDispatchEndpoint)
            .Select(p => p.EventId + ":" + p.Status)
            .ToListAsync();
        var executions = await probe.AutomationExecutions.IgnoreQueryFilters()
            .Select(e => e.Id + ":" + e.Status + ":attempt=" + e.AttemptCount)
            .ToListAsync();
        return $"processed=[{string.Join(",", processed)}] executions=[{string.Join(",", executions)}]";
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

            await Task.Delay(100);
        }

        return false;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class RecordingN8nClient : IN8nClient
    {
        private readonly ConcurrentQueue<N8nWebhookDispatchResult> _script = new();
        private int _callCount;

        public void Queue(N8nWebhookOutcome outcome, string? error = null) =>
            _script.Enqueue(new N8nWebhookDispatchResult(outcome, error));

        public Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
            string webhookPath,
            string payload,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            var result = _script.TryDequeue(out var next)
                ? next
                : new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null);
            return Task.FromResult(result);
        }

        public int CallCount => _callCount;
    }

    private sealed class RecordingDeduplicationStore(IMessageDeduplicationStore inner) : IMessageDeduplicationStore
    {
        private static readonly ConcurrentDictionary<(Guid EventId, string ConsumerName), int> Claims = new();

        public static int ClaimAttempts(Guid eventId, string consumerName) =>
            Claims.TryGetValue((eventId, consumerName), out var count) ? count : 0;

        public static void Reset() => Claims.Clear();

        public Task<bool> IsProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken) =>
            inner.IsProcessedAsync(messageId, consumerName, cancellationToken);

        public async Task<bool> TryClaimProcessingAsync(
            Guid messageId,
            string consumerName,
            string messageName,
            int messageVersion,
            Guid? sourceEventId,
            Guid? workspaceId,
            CancellationToken cancellationToken)
        {
            Claims.AddOrUpdate((messageId, consumerName), 1, (_, count) => count + 1);
            return await inner.TryClaimProcessingAsync(
                messageId, consumerName, messageName, messageVersion, sourceEventId, workspaceId, cancellationToken);
        }

        public void MarkSucceeded(Guid messageId, string consumerName, DateTimeOffset processedAt) =>
            inner.MarkSucceeded(messageId, consumerName, processedAt);
    }
}