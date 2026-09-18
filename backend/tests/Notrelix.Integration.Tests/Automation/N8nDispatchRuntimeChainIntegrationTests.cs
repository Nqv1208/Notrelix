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
/// DeduplicationConsumeFilter (consumer-owned path) → the real
/// N8nDispatchConsumer with its own transaction + RLS → the provider adapter
/// port. Exactly the shipped graph; only the provider port and the dedup/claim
/// store are replaced with a recording fake. Proves: (1) an unknown provider
/// outcome settles the execution as Failed with a reconciliation marker and
/// never auto re-fires, and (2) a retryable outcome persists durable attempt
/// evidence across real MassTransit retries (the first attempt's attempt-count
/// survives before the retry re-runs the consumer without re-acquiring under a
/// new identity).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class N8nDispatchRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string N8nDispatchEndpoint = N8nDispatchProtocolEndpoints.DispatchEndpointName;

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

    private async Task<ChainGraph> SeedRuleAndExecutionAsync(bool urlOnlyConfiguration)
    {
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"n8n-runtime-{Guid.NewGuid():N}@example.com", "Runtime N8N", "hashed", now, true);
        var account = Domain.Accounts.Accounts.Account.Create(
            "Runtime N8N Account", $"runtime-{Guid.NewGuid():N}",
            Domain.Accounts.Accounts.AccountType.Team, ownerId, now);
        var workspace = Workspace.Create(account.Id, ownerId, "Runtime WS", $"runtime-{Guid.NewGuid():N}", now);

        var actionConfiguration = urlOnlyConfiguration
            ? """{"url":"https://example.com/hooks/generic"}"""
            : """{"webhookPath":"notrelix-card-assigned"}""";
        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemAssigned"),
            AutomationActionDefinition.Create("Webhook", actionConfiguration));
        var rule = AutomationRule.Create(account.Id, workspace.Id, "Runtime alert", config, ownerId, now);
        rule.Enable(ownerId, now);
        var execution = AutomationExecution.Create(account.Id, workspace.Id, rule.Id, Guid.NewGuid(), now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now));
        seed.AutomationRules.Add(rule);
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();

        return new ChainGraph(account.Id, workspace.Id, rule.Id, execution.Id);
    }

    [Fact]
    public async Task UnknownOutcome_SettlesFailedWithReconciliation_AndNeverReFires()
    {
        var graph = await SeedRuleAndExecutionAsync(urlOnlyConfiguration: false);
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
        var graph = await SeedRuleAndExecutionAsync(urlOnlyConfiguration: false);
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

    [Fact]
    public async Task InvalidConfiguration_SettlesTerminalFailure_WithoutInvokingProvider()
    {
        var graph = await SeedRuleAndExecutionAsync(urlOnlyConfiguration: true);
        var providerN8n = new RecordingN8nClient();
        var tenantRecorder = new TenantObservationRecorder();

        await using var provider = BuildProvider(providerN8n, tenantRecorder);
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
            stored!.Status.Should().Be(AutomationExecutionStatus.Failed,
                "a Domain-valid rule that is not n8n-dispatchable settles terminally during prepare");
            stored.Error.Should().Contain("webhookPath",
                "the terminal error names the missing configuration member");

            providerN8n.CallCount.Should().Be(0,
                "the provider must never be invoked when the prepare phase settles the intent");
            RecordingDeduplicationStore.ClaimAttempts(message.EventId, N8nDispatchEndpoint).Should().Be(1,
                "the config defect settles in one delivery; no redelivery is requested");

            tenantRecorder.ObservedWorkspaceSet.Should().BeTrue(
                "TenantContextConsumeFilter must restore the Workspace tenant before the consumer pipe runs");
            tenantRecorder.LastWorkspaceAccountId.Should().Be(graph.AccountId,
                "the dispatch consumer must observe the authoritative AccountId");
            tenantRecorder.LastWorkspaceId.Should().Be(graph.WorkspaceId,
                "the dispatch consumer must observe the authoritative WorkspaceId");
            tenantRecorder.LastWorkspaceIsSystem.Should().BeFalse(
                "a Workspace-scoped dispatch must not execute its consumer as System");
            tenantRecorder.ClearedAfterWorkspace.Should().BeTrue(
                "TenantContextConsumeFilter must clear tenant state after consume completion");
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

    private ServiceProvider BuildProvider(IN8nClient providerN8n, TenantObservationRecorder? tenantRecorder = null)
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

        if (tenantRecorder is not null)
        {
            builder.Services.Replace(
                ServiceDescriptor.Scoped<Notrelix.Application.Common.Context.ICurrentTenantContext>(_ =>
                    new RecordingCurrentTenantContext(
                        new Notrelix.Infrastructure.Identity.Services.CurrentTenantContext(), tenantRecorder)));
        }

        builder.Services.Replace(ServiceDescriptor.Singleton<IN8nClient>(providerN8n));

        builder.Services.Replace(
            ServiceDescriptor.Scoped<IMessageDeduplicationStore>(sp =>
                new RecordingDeduplicationStore(
                    new MessageDeduplicationStore(
                        sp.GetRequiredService<ApplicationDbContext>(),
                        sp.GetRequiredService<Notrelix.Application.Common.Time.IDateTimeProvider>(),
                        sp.GetRequiredService<MetricsService>()))));

        builder.Services.Replace(
            ServiceDescriptor.Scoped<IProviderEffectClaimStore>(sp =>
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

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }
        public bool ClearedAfterWorkspace { get; private set; }

        public void RecordWorkspace(Guid accountId, Guid workspaceId, bool isSystemContext)
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = true;
                LastWorkspaceAccountId = accountId;
                LastWorkspaceId = workspaceId;
                LastWorkspaceIsSystem = isSystemContext;
            }
        }

        public void RecordClear()
        {
            lock (_gate)
            {
                if (ObservedWorkspaceSet)
                {
                    ClearedAfterWorkspace = true;
                }
            }
        }
    }

    private sealed class RecordingCurrentTenantContext(Notrelix.Infrastructure.Identity.Services.CurrentTenantContext inner, TenantObservationRecorder recorder)
        : Notrelix.Application.Common.Context.ICurrentTenantContext
    {
        public Guid? AccountId => inner.AccountId;
        public Guid? WorkspaceId => inner.WorkspaceId;
        public Guid? UserId => inner.UserId;
        public bool IsSystemContext => inner.IsSystemContext;
        public bool IsResolved => inner.IsResolved;

        public Guid RequireAccountId() => inner.RequireAccountId();
        public Guid RequireWorkspaceId() => inner.RequireWorkspaceId();
        public Guid RequireUserId() => inner.RequireUserId();

        public void SetUser(Guid userId) => inner.SetUser(userId);
        public void SetAccountHint(Guid accountId) => inner.SetAccountHint(accountId);
        public void SetAccount(Guid accountId, Guid? userId) => inner.SetAccount(accountId, userId);

        public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId)
        {
            inner.SetWorkspace(accountId, workspaceId, userId);
            recorder.RecordWorkspace(accountId, workspaceId, inner.IsSystemContext);
        }

        public void SetSystem() => inner.SetSystem();

        public void Clear()
        {
            inner.Clear();
            recorder.RecordClear();
        }
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

    private sealed class RecordingDeduplicationStore(MessageDeduplicationStore inner)
        : IMessageDeduplicationStore, IProviderEffectClaimStore
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

        public async Task<bool> TryAcquireClaimAsync(
            Guid messageId,
            string consumerName,
            string messageName,
            int messageVersion,
            Guid? sourceEventId,
            Guid? workspaceId,
            CancellationToken cancellationToken)
        {
            Claims.AddOrUpdate((messageId, consumerName), 1, (_, count) => count + 1);
            return await inner.TryAcquireClaimAsync(
                messageId, consumerName, messageName, messageVersion, sourceEventId, workspaceId, cancellationToken);
        }

        public Task<MessageClaimInspection> InspectClaimAsync(
            Guid messageId,
            string consumerName,
            CancellationToken cancellationToken) =>
            inner.InspectClaimAsync(messageId, consumerName, cancellationToken);

        public Task<bool> TryReleaseProcessingClaimAsync(
            Guid messageId,
            string consumerName,
            CancellationToken cancellationToken) =>
            inner.TryReleaseProcessingClaimAsync(messageId, consumerName, cancellationToken);

        public Task<bool> TryMarkClaimSucceededAsync(
            Guid messageId,
            string consumerName,
            DateTimeOffset processedAt,
            CancellationToken cancellationToken) =>
            inner.TryMarkClaimSucceededAsync(messageId, consumerName, processedAt, cancellationToken);
    }
}