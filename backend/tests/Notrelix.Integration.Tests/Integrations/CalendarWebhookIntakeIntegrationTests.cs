using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Integrations.Webhooks;
using Notrelix.Infrastructure.Options;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integrations;

/// <summary>
/// TAC-AI-FLOW-07 / C6 — the verified inbound calendar webhook intake over
/// real PostgreSQL: the handler resolves the per-connection WebhookPath
/// binding BEFORE any payload trust; the signature+timestamp verification
/// gates every callback; only after successful verification does the handler
/// derive the owning Account/Workspace and adopt it as the derived tenant for
/// the receipt; every fail-closed branch (unknown path, inactive binding,
/// provider mismatch, failed verification) rejects without adopting any tenant.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarWebhookIntakeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private const string Provider = "google";
    private const string ProviderSecret = "calendar-google-webhook-secret";
    private const string WebhookPath = "test-webhook-path-abc123";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CalendarWebhookIntakeIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static (string Body, string Signature, string Timestamp) SignedCallback(
        string externalEventId, DateTimeOffset occurredAt)
    {
        var body = JsonSerializer.Serialize(new { eventId = externalEventId, kind = "calendar#event" });
        var timestamp = occurredAt.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return (body, signature, timestamp);
    }

    private async Task<(Guid ConnectionId, Guid AccountId, Guid WorkspaceId)> SeedBindingAsync(
        string webhookPath,
        CalendarProvider provider = CalendarProvider.Google,
        bool active = true,
        IntegrationConnectionStatus connectionStatus = IntegrationConnectionStatus.Active)
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var integrationProvider = provider == CalendarProvider.Outlook
            ? IntegrationProvider.Microsoft
            : IntegrationProvider.Google;
        var connection = IntegrationConnection.Create(
            accountId, workspaceId, integrationProvider, Guid.NewGuid(), Now);
        if (connectionStatus != IntegrationConnectionStatus.Active)
        {
            switch (connectionStatus)
            {
                case IntegrationConnectionStatus.Revoked:
                    connection.Disconnect(Guid.NewGuid(), Now);
                    break;
                case IntegrationConnectionStatus.Expired:
                    connection.MarkExpired(Guid.NewGuid(), Now);
                    break;
                case IntegrationConnectionStatus.Error:
                    connection.MarkError("test error", Guid.NewGuid(), Now);
                    break;
            }
        }

        var binding = CalendarIntegration.Create(
            accountId, workspaceId, connection.Id, webhookPath,
            provider, CalendarSyncDirection.Pull, Guid.NewGuid(), Now);
        if (!active) binding.Deactivate(Guid.NewGuid(), Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.IntegrationConnections.Add(connection);
        seed.CalendarIntegrations.Add(binding);
        await seed.SaveChangesAsync();
        return (connection.Id, accountId, workspaceId);
    }

    private HandleCalendarWebhookCommandHandler CreateHandler(
        FakeCurrentTenantContext? tenant = null,
        Notrelix.Application.Common.Messaging.IIntegrationEventCollector? collector = null)
    {
        var scopeTenant = tenant ?? SystemTenant();
        var context = _db.CreateContext(scopeTenant);
        var options = Microsoft.Extensions.Options.Options.Create(new Notrelix.Infrastructure.Options.CalendarWebhookOptions
        {
            Providers = new Dictionary<string, Notrelix.Infrastructure.Options.CalendarWebhookOptions.CalendarWebhookProviderOptions>
            {
                [Provider] = new() { Enabled = true, SharedSecret = ProviderSecret },
            },
        });
        var encryptor = new Mock<Notrelix.Application.Common.Security.ISecretEncryptor>();
        encryptor.Setup(e => e.Protect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((plain, purpose) => $"protected:{purpose}:{plain}");
        encryptor.Setup(e => e.Unprotect(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
            .Returns<string, string>((cipher, _) => cipher[(cipher.IndexOf(':') + 1)..][(cipher.IndexOf(':') + 1)..]);

        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions
            {
                Enabled = true,
                SetSessionContext = true,
                ApplyPoliciesOnStartup = false
            }),
            scopeTenant);

        return new HandleCalendarWebhookCommandHandler(
            new CalendarWebhookBindingResolver(context, scopeTenant, rls),
            new CalendarWebhookVerifier(new FixedClock(Now), options),
            new CalendarWebhookIntake(context, encryptor.Object),
            collector ?? new IntegrationEventCollector(),
            new FixedClock(Now));
    }

    [Fact]
    public async Task VerifiedCallback_IsAccepted_CapturedNonTerminal_AndEnqueued()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var collector = new IntegrationEventCollector();
        var handler = CreateHandler(tenant, collector);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeTrue("a correctly signed callback must be accepted");

        tenant.AccountId.Should().Be(accountId,
            "the tenant must be derived from the verified binding, not from the payload");
        tenant.WorkspaceId.Should().Be(workspaceId);

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
        receipt.Status.Should().Be("Captured",
            "Wave E: the bootstrap claim is NON-terminal — the tenant-scoped processing seam decides the terminal state");
        receipt.ConnectionId.Should().Be(connectionId,
            "the accepted receipt carries the trusted binding's ConnectionId, not payload-derived identity");
        receipt.PayloadHash.Should().Be(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))),
            "the payload hash is SHA-256 over the exact verified raw bytes");
        receipt.ProtectedPayload.Should().NotBeNullOrEmpty(
            "the raw payload is persisted protected, not plaintext");
        receipt.ProtectedPayload.Should().NotBe(body);

        // Wave E: the bootstrap enqueued one provider-neutral processing intent
        // carrying the stable claim identity, provenance and derived tenant.
        var batch = collector.CapturePending();
        var message = batch.Events.OfType<Notrelix.Application.Events.Integrations.CalendarWebhookProcessingRequestedV1>()
            .Should().ContainSingle().Subject;
        message.ReceiptId.Should().Be(receipt.Id);
        message.ConnectionId.Should().Be(connectionId);
        message.Provider.Should().Be(Provider);
        message.ExternalEventId.Should().Be(externalEventId);
        message.PayloadHash.Should().Be(receipt.PayloadHash);
        message.ReceivedAt.Should().Be(Now);
        message.AccountIdValue.Should().Be(accountId, "the envelope carries the derived tenant, never a payload value");
        message.WorkspaceIdValue.Should().Be(workspaceId);
        message.WorkspaceId.Should().Be(workspaceId);
        message.AccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task AppRole_ForceRlsBootstrap_ResolvesOnlyRouteBoundConnection()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);

        await using var context = _db.CreateContext(SystemTenant());
        await new Notrelix.Infrastructure.Data.Rls.RlsPolicyApplier(
            context,
            NullLogger<Notrelix.Infrastructure.Data.Rls.RlsPolicyApplier>.Instance)
            .ApplyAsync();

        await context.Database.OpenConnectionAsync();
        await using (var role = context.Database.GetDbConnection().CreateCommand())
        {
            role.CommandText = "SET ROLE notrelix_app";
            await role.ExecuteNonQueryAsync();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var tenant = SystemTenant();
        var rls = new RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(new RlsOptions
            {
                Enabled = true,
                SetSessionContext = true,
                ApplyPoliciesOnStartup = false
            }),
            tenant);
        var resolver = new CalendarWebhookBindingResolver(context, tenant, rls);

        var resolved = await resolver.ResolveActiveAsync(WebhookPath, CancellationToken.None);

        resolved.Should().NotBeNull(
            "the app role must resolve the exact WebhookPath through the narrow bootstrap policy under FORCE RLS");
        resolved!.ConnectionId.Should().Be(connectionId);
        resolved.AccountId.Should().Be(accountId);
        resolved.WorkspaceId.Should().Be(workspaceId);

        (await resolver.ResolveActiveAsync(WebhookPath + "-not-found", CancellationToken.None))
            .Should().BeNull("the locator policy must not expose another row without an exact route token");

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task PayloadAccountWorkspaceAreIgnored_TenantAndConnectionDerivedFromBinding()
    {
        var (connectionId, accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();

        // The verifier signs the EXACT raw bytes; the signature is computed
        // over the same payload that is sent.
        var payloadWithFakeTenant = JsonSerializer.Serialize(new
        {
            eventId = externalEventId,
            kind = "calendar#event",
            accountId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
            connectionId = Guid.NewGuid(),
        });
        var timestamp = Now.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payloadWithFakeTenant}")));

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);

        // Even if the payload contained misleading account/workspace/connection
        // fields (the HTTP surface does not even accept them), the handler
        // ignores them and derives tenant AND ConnectionId exclusively from the
        // verified binding.
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, payloadWithFakeTenant, WebhookPath),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        tenant.AccountId.Should().Be(accountId,
            "the handler must derive tenant from the binding, never from the payload");
        tenant.WorkspaceId.Should().Be(workspaceId);

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
        receipt.ConnectionId.Should().Be(connectionId,
            "a payload-supplied connectionId must never override the trusted binding identity");
    }

    [Fact]
    public async Task InvalidSignature_IsRejected_BeforeAnyBusinessEffect()
    {
        await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, _, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, "sha256=DEADBEEF", timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a tampered callback must be rejected");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        // Tenant must NOT have been adopted from the binding.
        tenant.AccountId.Should().BeNull("an invalid signature must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.Status == "Rejected");
        receipt.FailureReason.Should().Contain("Signature mismatch");
        receipt.Status.Should().Be("Rejected",
            "a rejected callback must never become business processing state");
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .AnyAsync(r => r.ExternalEventId == externalEventId && (r.Status == "Processed" || r.Status == "Captured")))
            .Should().BeFalse("the rejected callback must produce no accepted technical receipt");
    }

    [Fact]
    public async Task StaleTimestamp_IsRejected_AsReplay()
    {
        await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, _) = SignedCallback(externalEventId, Now.AddMinutes(-30));

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, Now.AddMinutes(-30).ToUnixTimeSeconds().ToString(), body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a stale callback must be rejected as a replay attempt");

        tenant.AccountId.Should().BeNull("a replay must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task UnknownWebhookPath_IsRejected_NoTenantAdopted()
    {
        // No binding seeded — the webhook path does not exist.
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, "unknown-webhook-path"), CancellationToken.None);

        result.Succeeded.Should().BeFalse("an unknown webhook path must fail closed");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        tenant.AccountId.Should().BeNull("an unknown path must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task InactiveBinding_IsRejected_NoTenantAdopted()
    {
        await SeedBindingAsync(WebhookPath, active: false);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("an inactive binding must fail closed");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        tenant.AccountId.Should().BeNull("an inactive binding must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task DeletedBinding_IsRejected_NoTenantAdopted()
    {
        await SeedBindingAsync(WebhookPath);
        // Soft-delete the binding.
        await using (var ctx = _db.CreateContext(SystemTenant()))
        {
            var binding = await ctx.CalendarIntegrations.IgnoreQueryFilters()
                .SingleAsync(ci => ci.WebhookPath == WebhookPath);
            binding.Delete(Guid.NewGuid(), Now, "test delete");
            await ctx.SaveChangesAsync();
        }

        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a deleted binding must fail closed");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        tenant.AccountId.Should().BeNull("a deleted binding must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task ProviderMismatch_IsRejected_NoTenantAdopted()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connection = IntegrationConnection.Create(
            accountId, workspaceId, IntegrationProvider.Microsoft, Guid.NewGuid(), Now);
        var binding = CalendarIntegration.Create(
            accountId, workspaceId, connection.Id, WebhookPath,
            CalendarProvider.Outlook, CalendarSyncDirection.Pull, Guid.NewGuid(), Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.IntegrationConnections.Add(connection);
        seed.CalendarIntegrations.Add(binding);
        await seed.SaveChangesAsync();

        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        // Route says "google" but binding is Outlook → mismatch.
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a provider mismatch must fail closed");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        tenant.AccountId.Should().BeNull("a provider mismatch must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateDelivery_SameExternalEventId_OneNonTerminalReceipt_OneEnqueuedMessage()
    {
        await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);
        var collector = new IntegrationEventCollector();
        var handler = CreateHandler(collector: collector);

        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        // Redeliver the SAME verified callback: the intake accepts it
        // idempotently (HTTP-level) but produces no second receipt and no
        // second processing intent — the loser is a transport live-delivery
        // race, not a business duplicate.
        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue("the intake is idempotent at the transport boundary");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .CountAsync(r => r.ExternalEventId == externalEventId && r.Status == "Captured"))
            .Should().Be(1, "exactly one non-terminal receipt survives the duplicate delivery");

        collector.CapturePending().Events
            .OfType<Notrelix.Application.Events.Integrations.CalendarWebhookProcessingRequestedV1>()
            .Should().ContainSingle("exactly one processing intent is enqueued for the claimed delivery");
    }

    [Fact]
    public async Task ConcurrentDuplicateClaims_ResolveToExactlyOneAcceptedReceipt()
    {
        await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var first = CreateHandler();
        var second = CreateHandler();
        var command = new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath);

        var results = await Task.WhenAll(
            first.Handle(command, CancellationToken.None),
            second.Handle(command, CancellationToken.None));

        results.Should().OnlyContain(r => r.Succeeded,
            "the loser of the claim race must not surface an error to the provider");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .CountAsync(r => r.ExternalEventId == externalEventId && r.Status == "Captured"))
            .Should().Be(1, "exactly one non-terminal receipt survives concurrent delivery");
    }

    [Fact]
    public async Task SameConnection_SameProvider_DifferentExternalEventId_BothAccepted()
    {
        var (connectionId, _, _) = await SeedBindingAsync(WebhookPath);
        var firstEventId = Guid.NewGuid().ToString();
        var secondEventId = Guid.NewGuid().ToString();
        var handler = CreateHandler();

        var (body1, sig1, ts1) = SignedCallback(firstEventId, Now);
        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, sig1, ts1, body1, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        var (body2, sig2, ts2) = SignedCallback(secondEventId, Now);
        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, sig2, ts2, body2, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue("a distinct event on the same connection must be accepted");

        await using var verify = _db.CreateContext(SystemTenant());
        var receipts = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .Where(r => r.ConnectionId == connectionId && r.Status == "Captured")
            .ToListAsync();
        receipts.Select(r => r.ExternalEventId).Should().BeEquivalentTo(firstEventId, secondEventId);
    }

    [Fact]
    public async Task DifferentConnection_SameProvider_SameExternalEventId_BothAccepted()
    {
        // Two connections (two distinct calendars) on the same provider can
        // legitimately carry the SAME provider event-id string — the provider
        // event id only namespaces a delivery within a provider
        // calendar/connection. Connection-scoped dedup must accept BOTH; the
        // former provider-wide unique index would have wrongly dropped the
        // second calendar's event as a duplicate.
        var (connectionA, accountA, workspaceA) = await SeedBindingAsync("webhook-a-aaa111");
        var (connectionB, accountB, workspaceB) = await SeedBindingAsync("webhook-b-bbb222");
        connectionA.Should().NotBe(connectionB);
        accountA.Should().NotBe(accountB);
        workspaceA.Should().NotBe(workspaceB);

        var sharedExternalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(sharedExternalEventId, Now);

        var tenantA = SystemTenant();
        var handlerA = CreateHandler(tenantA);
        var tenantB = SystemTenant();
        var handlerB = CreateHandler(tenantB);

        (await handlerA.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, "webhook-a-aaa111"), CancellationToken.None))
            .Succeeded.Should().BeTrue("the first connection's calendar event must be accepted");
        (await handlerB.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, "webhook-b-bbb222"), CancellationToken.None))
            .Succeeded.Should().BeTrue("the second connection's distinct calendar event with the same id must also be accepted");

        tenantA.AccountId.Should().Be(accountA);
        tenantB.AccountId.Should().Be(accountB);

        await using var verify = _db.CreateContext(SystemTenant());
        var receipts = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .Where(r => r.ExternalEventId == sharedExternalEventId && r.Status == "Captured")
            .ToListAsync();
        receipts.Should().HaveCount(2,
            "connection-scoped dedup must keep two receipts for the same event id on different connections");
        receipts.Select(r => r.ConnectionId).Should().BeEquivalentTo(
            new Guid?[] { connectionA, connectionB });
    }

    [Fact]
    public async Task RevokedConnection_IsRejected_NoTenantNoAcceptedReceipt()
    {
        var (connectionId, _, _) = await SeedBindingAsync(
            WebhookPath, connectionStatus: IntegrationConnectionStatus.Revoked);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a revoked connection must fail closed");
        result.Errors.Should().Contain("integrations.webhook.rejected");
        tenant.AccountId.Should().BeNull("a revoked connection must not derive any tenant");
        tenant.WorkspaceId.Should().BeNull();

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .AnyAsync(r => r.ExternalEventId == externalEventId && (r.Status == "Processed" || r.Status == "Captured")))
            .Should().BeFalse("a revoked connection must produce no accepted receipt");
    }

    [Fact]
    public async Task ExpiredConnection_IsRejected_NoTenantAdopted()
    {
        await SeedBindingAsync(WebhookPath, connectionStatus: IntegrationConnectionStatus.Expired);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeFalse("an expired connection must fail closed");
        tenant.AccountId.Should().BeNull();
        tenant.WorkspaceId.Should().BeNull();
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
