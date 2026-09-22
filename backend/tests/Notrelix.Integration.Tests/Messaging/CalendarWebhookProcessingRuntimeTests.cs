using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Application.Features.Integrations.Calendar.Processing;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Integrations.Webhooks;
using Notrelix.Infrastructure.Options;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC v2.6 WAVE-E — the Wave-E split production graph: the bootstrap commits a
/// NON-TERMINAL "Captured" claim plus exactly one provider-neutral
/// <c>calendar_webhook_processing_requested</c> outbox row in the same real
/// transaction, and the real delivery chain (outbox dispatcher → MassTransit
/// InMemory receive pipeline → TenantContextConsumeFilter → real
/// <see cref="CalendarWebhookProcessingRequestedConsumer"/>) restores the
/// workspace tenant before running the consumer, then reconciles the
/// Integrations CalendarEvent target and durably records "Processed" only
/// after that target mutation commits. A duplicate delivery converges to the
/// same single receipt and single enrollment.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarWebhookProcessingRuntimeTests : IAsyncLifetime
{
    private const string Provider = "google";
    private const string ProviderSecret = "calendar-google-webhook-secret";
    private const string WebhookPath = "rt-test-webhook-path-abc123";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CalendarWebhookProcessingRuntimeTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task VerifiedCallback_EnrollsExactlyOneOutboxRow_AndConsumerReconcilesUnderDerivedTenant()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid();
        var (body, signature, timestamp) = SignedCallback(externalEventId, targetId);

        var recorder = new TenantObservationRecorder();
        var transport = new RuntimeTransportObservation();
        // Exercise the DB/JSON timestamp boundary with a value that contains
        // sub-microsecond .NET precision. PostgreSQL stores timestamp values at
        // microsecond precision, so the production path must canonicalize this
        // value before persisting the receipt and outbox payload.
        await using var provider = BuildProvider(
            recorder,
            transport,
            DateTimeOffset.UtcNow.AddTicks(1));

        Guid receiptId;
        Guid outboxId;

        // 1. Bootstrap through the canonical request pipeline with the real
        //    webhook vertical. The claim + the outbox row commit atomically in
        //    the data-session transaction.
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath));

            result.Succeeded.Should().BeTrue("the data session commits the accepted claim");
        }

        // 2. Durable bootstrap state: one Captured (non-terminal) claim and
        //    exactly one outbox row with the authoritative tenant envelope.
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var outbox = await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .SingleAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId);
            outboxId = outbox.Id;
            outbox.SchemaVersion.Should().Be(1);
            outbox.AccountId.Should().Be(accountId,
                "the outbox row must carry the authoritative tenant envelope");
            outbox.WorkspaceId.Should().Be(workspaceId);
            receiptId = outbox.PayloadJson.RootElement.GetProperty("receiptId").GetGuid();

            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == receiptId);
            receipt.Status.Should().Be("Captured",
                "the bootstrap decides a non-terminal claim only; the consumer decides the terminal state");
            receipt.ProcessedAt.Should().BeNull();
            receipt.ConnectionId.Should().Be(connectionId);
        }

        // 3. Start the real delivery chain and let the consumer decide.
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        recorder.Reset();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var dispatched = await WaitForOutboxProcessedAsync(outboxId);
            dispatched.Should().BeTrue("the dispatcher must deliver the committed processing intent");

            var processed = await WaitForReceiptStatusAsync(receiptId, outboxId, "Processed", transport, recorder);
            processed.Reached.Should().BeTrue(
                "the real consumer must commit the CalendarEvent target. Snapshot: {0}",
                processed.Snapshot);

            (await OutboxRowCountAsync(accountId, workspaceId)).Should().Be(1,
                "exactly one processing enrollment per callback");

            await using var final = _db.CreateContext(SystemTenant());
            var terminal = await final.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Id == receiptId);
            terminal.Status.Should().Be("Processed");
            terminal.ProcessedAt.Should().NotBeNull();

            var calendarEvent = await final.CalendarEvents.IgnoreQueryFilters()
                .SingleAsync(e => e.ExternalEventId == externalEventId);
            calendarEvent.Target.ResourceId.Should().Be(targetId);
            calendarEvent.Target.WorkspaceId.Should().Be(workspaceId);
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before the consumer runs");
        recorder.LastWorkspaceAccountId.Should().Be(accountId);
        recorder.LastWorkspaceId.Should().Be(workspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse(
            "a workspace-scoped processing intent must not run its consumer as System");
        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    [Fact]
    public async Task DuplicateDelivery_ConvergesToSingleReceipt_AndSingleConsumerDecision()
    {
        var (_, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Guid.NewGuid());

        var recorder = new TenantObservationRecorder();
        var transport = new RuntimeTransportObservation();
        await using var provider = BuildProvider(recorder, transport);

        Guid receiptId;
        Guid outboxId;

        // The same callback is delivered twice (each delivery is its own request
        // scope, as in HTTP): the payload-hash/connection claim must converge
        // both into a single non-terminal receipt and a single outbox enrollment.
        var command = new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath);
        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            (await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();
            tenant.SetSystem();
            (await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue("a duplicate delivery is an idempotent acceptance, not an error");
        }

        {
            await using var probe = _db.CreateContext(SystemTenant());
            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
            receipt.Status.Should().Be("Captured",
                "two deliveries produce exactly one non-terminal claim");
            receiptId = receipt.Id;

            var outbox = await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .SingleAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId);
            outboxId = outbox.Id;
            (await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId)).Should().Be(1,
                "two deliveries produce exactly one outbox enrollment");
        }

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        recorder.Reset();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            var processed = await WaitForReceiptStatusAsync(receiptId, outboxId, "Processed", transport, recorder);
            processed.Reached.Should().BeTrue(
                "the consumer must reach the single durable target reconciliation. Snapshot: {0}",
                processed.Snapshot);

            await using var final = _db.CreateContext(SystemTenant());
            (await final.InboundWebhookReceipts.IgnoreQueryFilters()
                .CountAsync(r => r.ExternalEventId == externalEventId)).Should().Be(1,
                "no duplicate receipt is created by a redelivered callback");
            (await final.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                               && m.WorkspaceId == workspaceId)).Should().Be(1,
                "no duplicate enrollment is created by a redelivered callback");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "the single consumer run must still execute under the restored workspace tenant");
        recorder.LastWorkspaceAccountId.Should().Be(accountId);
        recorder.LastWorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public async Task ProcessingUseCase_RejectsMalformedPayload_AsTerminalFailure()
    {
        var (connectionId, _, workspaceId) = await SeedBindingAsync(WebhookPath);
        await using var db = _db.CreateContext(SystemTenant());
        var useCase = new CalendarWebhookProcessingUseCase(db);

        var outcome = await useCase.ProcessAsync(
            new CalendarWebhookProcessingInput(
                Guid.NewGuid(),
                connectionId,
                workspaceId,
                Provider,
                "external-event",
                "payload-hash",
                DateTimeOffset.UtcNow,
                "{\"eventId\":\"external-event\",\"resourceKind\":\"work-management.board-item\"}"),
            CancellationToken.None);

        outcome.Should().Be(CalendarWebhookProcessingOutcome.TerminalFailure);
    }

    [Fact]
    public async Task ProcessingUseCase_AcceptsValidMapping_AsCompleted()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid();
        var (body, _, _) = SignedCallback(externalEventId, targetId);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, Guid.NewGuid());

        await using var db = _db.CreateContext(tenant);
        var outcome = await new CalendarWebhookProcessingUseCase(db).ProcessAsync(
            new CalendarWebhookProcessingInput(
                Guid.NewGuid(),
                connectionId,
                workspaceId,
                Provider,
                externalEventId,
                "payload-hash",
                DateTimeOffset.UtcNow,
                body),
            CancellationToken.None);

        outcome.Should().Be(CalendarWebhookProcessingOutcome.Completed);
    }

    [Fact]
    public async Task ProcessingUseCase_RejectsConflictingMapping_AsTerminalFailure()
    {
        var (connectionId, _, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = "external-event";
        var originalTargetId = Guid.NewGuid();
        var conflictingTargetId = Guid.NewGuid();
        var resourceKind = ResourceKind.Create("work-management.board-item");

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            var integration = await seed.CalendarIntegrations
                .IgnoreQueryFilters()
                .Include(x => x.EventLinks)
                .SingleAsync(x => x.ConnectionId == connectionId);
            integration.LinkEvent(originalTargetId, externalEventId, "etag-1");
            seed.CalendarEvents.Add(CalendarEvent.Create(
                integration.Id,
                externalEventId,
                ResourceRef.Create(resourceKind, originalTargetId, workspaceId),
                CalendarSyncFingerprint.Create("original", null)));
            await seed.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(new
        {
            eventId = externalEventId,
            resourceKind = resourceKind.Value,
            resourceId = conflictingTargetId,
        });

        await using var db = _db.CreateContext(SystemTenant());
        var outcome = await new CalendarWebhookProcessingUseCase(db).ProcessAsync(
            new CalendarWebhookProcessingInput(
                Guid.NewGuid(),
                connectionId,
                workspaceId,
                Provider,
                externalEventId,
                "payload-hash",
                DateTimeOffset.UtcNow,
                payload),
            CancellationToken.None);

        outcome.Should().Be(CalendarWebhookProcessingOutcome.TerminalFailure);
    }

    // ── composition -----------------------------------------------------------

    private ServiceProvider BuildProvider(
        TenantObservationRecorder recorder,
        RuntimeTransportObservation transport,
        DateTimeOffset? fixedNow = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["Messaging:Transport"] = "InMemory",
                // Each runtime test provider owns an in-memory bus. A unique
                // endpoint namespace prevents parallel integration tests from
                // competing for the same MassTransit loopback endpoints.
                ["Messaging:EndpointPrefix"] = $"notrelix-calendar-{Guid.NewGuid():N}",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
            })
            .Build();

        var encryptor = new Mock<ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((plain, purpose) => $"protected:{purpose}:{plain}");
        encryptor.Setup(e => e.Unprotect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((cipher, _) => cipher.Split(':', 3)[2]);

        var webhookOptions = Options.Create(new CalendarWebhookOptions
        {
            Providers = new Dictionary<string, CalendarWebhookOptions.CalendarWebhookProviderOptions>
            {
                [Provider] = new() { Enabled = true, SharedSecret = ProviderSecret },
            },
        });

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddOptions();
        services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        services.AddSingleton(environment.Object);

        services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser());
        services.AddScoped<ICurrentTenantContext>(_ =>
            new RecordingCurrentTenantContext(new CurrentTenantContext(), recorder));

        // The granular production graph.
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddObservability(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddCrossContextBindings();

        // Application-provided runtime seams this graph must bind explicitly:
        // the domain-event outbox collector and the Integrations processing seam.
        services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        services.AddScoped<ICalendarWebhookProcessingUseCase, CalendarWebhookProcessingUseCase>();
        if (fixedNow is not null)
        {
            services.AddScoped<IDateTimeProvider>(_ =>
                FakeDateTimeProvider.WithFixedTime(fixedNow.Value));
        }

        // The canonical request pipeline around the REAL webhook vertical.
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(HandleCalendarWebhookCommand).Assembly));
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddSingleton<ISecretEncryptor>(encryptor.Object);
        services.AddSingleton(webhookOptions);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.None);
        services.AddSingleton<ICurrentCredentialContext>(credentialMock.Object);

        services.AddScoped<ICalendarWebhookVerifier, CalendarWebhookVerifier>();
        services.AddScoped<ICalendarWebhookIntake, CalendarWebhookIntake>();
        services.AddScoped<ICalendarWebhookBindingResolver, CalendarWebhookBindingResolver>();
        services.AddScoped<CalendarWebhookVerifier>();
        services.AddScoped<CalendarWebhookIntake>();
        services.AddScoped<CalendarWebhookBindingResolver>();
        services.AddScoped<
            IRequestHandler<HandleCalendarWebhookCommand, Result>,
            HandleCalendarWebhookCommandHandler>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddSingleton<PipelineMetrics>();
        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddSingleton(transport);

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        ((IReceiveObserverConnector)bus).ConnectReceiveObserver(transport);
        return provider;
    }

    // ── seeding ---------------------------------------------------------------

    private async Task<(Guid ConnectionId, Guid AccountId, Guid WorkspaceId)> SeedBindingAsync(string webhookPath)
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connection = IntegrationConnection.Create(
            accountId, workspaceId, IntegrationProvider.Google, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var binding = CalendarIntegration.Create(
            accountId, workspaceId, connection.Id, webhookPath,
            CalendarProvider.Google, CalendarSyncDirection.Pull, Guid.NewGuid(), DateTimeOffset.UtcNow);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.IntegrationConnections.Add(connection);
        seed.CalendarIntegrations.Add(binding);
        await seed.SaveChangesAsync();
        return (connection.Id, accountId, workspaceId);
    }

    private static (string Body, string Signature, string Timestamp) SignedCallback(string externalEventId, Guid targetId)
    {
        var body = JsonSerializer.Serialize(new
        {
            eventId = externalEventId,
            kind = "calendar#event",
            resourceKind = "work-management.board-item",
            resourceId = targetId,
            title = "Calendar webhook target",
            dueDate = "2026-09-22T00:00:00Z",
            etag = "etag-1",
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return (body, signature, timestamp);
    }

    // ── polling helpers -------------------------------------------------------

    private async Task<long> OutboxRowCountAsync(Guid accountId, Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(m => m.MessageName == "integrations.calendar-webhook-processing-requested"
                          && m.AccountId == accountId
                          && m.WorkspaceId == workspaceId);
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid outboxId)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
                .AnyAsync(m => m.Id == outboxId && m.Status == "Processed");
        });
    }

    private async Task<ReceiptStatusWaitResult> WaitForReceiptStatusAsync(
        Guid receiptId,
        Guid outboxId,
        string status,
        RuntimeTransportObservation transport,
        TenantObservationRecorder recorder)
    {
        var reached = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == receiptId);
            return receipt?.Status == status;
        });

        return reached
            ? new ReceiptStatusWaitResult(true, string.Empty)
            : new ReceiptStatusWaitResult(
                false,
                await CaptureSnapshotAsync(receiptId, outboxId, transport, recorder));
    }

    private async Task<string> CaptureSnapshotAsync(
        Guid receiptId,
        Guid outboxId,
        RuntimeTransportObservation transport,
        TenantObservationRecorder recorder)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        var receipt = await probe.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleOrDefaultAsync(r => r.Id == receiptId);
        var outbox = await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.Id == outboxId);
        var attempt = outbox is null
            ? null
            : await probe.Set<OutboxDeliveryAttempt>().IgnoreQueryFilters()
                .Where(a => a.OutboxMessageId == outbox.Id)
                .OrderByDescending(a => a.AttemptNo)
                .Select(a => new { a.Status, a.ErrorCode })
                .FirstOrDefaultAsync();
        var processed = outbox is null
            ? []
            : await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
                .Where(e => e.EventId == outbox.EventId)
                .OrderBy(e => e.ConsumerName)
                .Select(e => $"{e.ConsumerName}:{e.Status}:{e.ErrorMessage ?? ""}")
                .ToListAsync();

        return string.Join(
            " | ",
            $"Receipt status={receipt?.Status ?? "missing"} failureCode={receipt?.FailureCode ?? "none"} terminalAt={receipt?.TerminalAt?.ToString("O") ?? "none"}",
            $"Outbox status={outbox?.Status ?? "missing"} retryCount={outbox?.RetryCount.ToString() ?? "none"} lockId={outbox?.LockId?.ToString() ?? "none"}",
            $"Attempt status={attempt?.Status ?? "none"} errorCode={attempt?.ErrorCode ?? "none"}",
            $"Processed=[{string.Join(",", processed)}]",
            $"MassTransit endpoint={transport.CalendarEndpoint ?? "none"} receive={transport.ReceiveCount} consume={transport.ConsumeCount} fault={transport.FaultCount}",
            $"Tenant observed={recorder.ObservedWorkspaceSet}");
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

    private sealed record ReceiptStatusWaitResult(bool Reached, string Snapshot);

    private sealed class RuntimeTransportObservation : IReceiveObserver
    {
        private readonly object _gate = new();

        public string? CalendarEndpoint { get; private set; }
        public int ReceiveCount { get; private set; }
        public int ConsumeCount { get; private set; }
        public int FaultCount { get; private set; }

        public Task PreReceive(ReceiveContext context)
        {
            RecordEndpoint(context.InputAddress, incrementReceive: true);
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context)
        {
            RecordEndpoint(context.InputAddress, incrementReceive: false);
            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            if (IsCalendar(context.ReceiveContext.InputAddress))
            {
                lock (_gate)
                {
                    ConsumeCount++;
                }
            }

            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(
            ConsumeContext<T> context,
            TimeSpan duration,
            string consumerType,
            Exception exception)
            where T : class
        {
            if (IsCalendar(context.ReceiveContext.InputAddress))
            {
                lock (_gate)
                {
                    FaultCount++;
                }
            }

            return Task.CompletedTask;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            if (IsCalendar(context.InputAddress))
            {
                lock (_gate)
                {
                    FaultCount++;
                }
            }

            return Task.CompletedTask;
        }

        private void RecordEndpoint(Uri? address, bool incrementReceive)
        {
            if (!IsCalendar(address))
            {
                return;
            }

            lock (_gate)
            {
                CalendarEndpoint = address!.AbsolutePath.Trim('/');
                if (incrementReceive)
                {
                    ReceiveCount++;
                }
            }
        }

        private static bool IsCalendar(Uri? address) =>
            address?.AbsolutePath.Contains(
                "integrations-calendar-webhook-processing-requested-v1",
                StringComparison.OrdinalIgnoreCase) == true;
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    // ── tenant observation (mirrors the frozen scoped-tenant-chain pattern) ──

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }
        public bool ClearedAfterWorkspace { get; private set; }

        public void Reset()
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = false;
                ClearedAfterWorkspace = false;
            }
        }

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

    private sealed class RecordingCurrentTenantContext : ICurrentTenantContext
    {
        private readonly CurrentTenantContext _inner;
        private readonly TenantObservationRecorder _recorder;

        public RecordingCurrentTenantContext(CurrentTenantContext inner, TenantObservationRecorder recorder)
        {
            _inner = inner;
            _recorder = recorder;
        }

        public Guid? AccountId => _inner.AccountId;
        public Guid? WorkspaceId => _inner.WorkspaceId;
        public Guid? UserId => _inner.UserId;
        public bool IsSystemContext => _inner.IsSystemContext;
        public bool IsResolved => _inner.IsResolved;

        public Guid RequireAccountId() => _inner.RequireAccountId();
        public Guid RequireWorkspaceId() => _inner.RequireWorkspaceId();
        public Guid RequireUserId() => _inner.RequireUserId();

        public void SetUser(Guid userId) => _inner.SetUser(userId);
        public void SetAccountHint(Guid accountId) => _inner.SetAccountHint(accountId);

        public void SetAccount(Guid accountId, Guid? userId) => _inner.SetAccount(accountId, userId);

        public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId)
        {
            _inner.SetWorkspace(accountId, workspaceId, userId);
            if (!_inner.IsSystemContext)
            {
                _recorder.RecordWorkspace(accountId, workspaceId, _inner.IsSystemContext);
            }
        }

        public void SetSystem()
        {
            _inner.SetSystem();
        }

        public void Clear()
        {
            _inner.Clear();
            _recorder.RecordClear();
        }
    }
}
