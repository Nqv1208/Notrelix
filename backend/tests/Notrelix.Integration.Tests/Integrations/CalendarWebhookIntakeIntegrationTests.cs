using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Infrastructure.Integrations.Webhooks;
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

    private async Task<(Guid AccountId, Guid WorkspaceId)> SeedBindingAsync(
        string webhookPath,
        CalendarProvider provider = CalendarProvider.Google,
        bool active = true)
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var binding = CalendarIntegration.Create(
            accountId, workspaceId, Guid.NewGuid(), webhookPath,
            provider, CalendarSyncDirection.Pull, Guid.NewGuid(), Now);
        if (!active) binding.Deactivate(Guid.NewGuid(), Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.CalendarIntegrations.Add(binding);
        await seed.SaveChangesAsync();
        return (accountId, workspaceId);
    }

    private HandleCalendarWebhookCommandHandler CreateHandler(FakeCurrentTenantContext? tenant = null)
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

        return new HandleCalendarWebhookCommandHandler(
            new CalendarWebhookBindingResolver(context, scopeTenant),
            new CalendarWebhookVerifier(new FixedClock(Now), options),
            new CalendarWebhookIntake(context, encryptor.Object, new FixedClock(Now)),
            new FixedClock(Now));
    }

    [Fact]
    public async Task VerifiedCallback_IsAccepted_AndProcessed()
    {
        var (accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None);

        result.Succeeded.Should().BeTrue("a correctly signed callback must be accepted");

        tenant.AccountId.Should().Be(accountId,
            "the tenant must be derived from the verified binding, not from the payload");
        tenant.WorkspaceId.Should().Be(workspaceId);

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
        receipt.Status.Should().Be("Processed");
        receipt.PayloadHash.Should().Be(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))),
            "the payload hash is SHA-256 over the exact verified raw bytes");
        receipt.ProtectedPayload.Should().NotBeNullOrEmpty(
            "the raw payload is persisted protected, not plaintext");
        receipt.ProtectedPayload.Should().NotBe(body);
    }

    [Fact]
    public async Task PayloadAccountWorkspaceAreIgnored_TenantDerivedFromBinding()
    {
        var (accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();

        // The verifier signs the EXACT raw bytes; the signature is computed
        // over the same payload that is sent.
        var payloadWithFakeTenant = JsonSerializer.Serialize(new
        {
            eventId = externalEventId,
            kind = "calendar#event",
            accountId = Guid.NewGuid(),
            workspaceId = Guid.NewGuid(),
        });
        var timestamp = Now.ToUnixTimeSeconds().ToString();
        var signature = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(ProviderSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payloadWithFakeTenant}")));

        var tenant = SystemTenant();
        var handler = CreateHandler(tenant);

        // Even if the payload contained misleading account/workspace fields
        // (the HTTP surface does not even accept them), the handler ignores
        // them and derives tenant exclusively from the verified binding.
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, payloadWithFakeTenant, WebhookPath),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        tenant.AccountId.Should().Be(accountId,
            "the handler must derive tenant from the binding, never from the payload");
        tenant.WorkspaceId.Should().Be(workspaceId);
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
            .AnyAsync(r => r.ExternalEventId == externalEventId && r.Status == "Processed"))
            .Should().BeFalse("the rejected callback must produce no processed technical receipt");
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
        var (accountId, workspaceId) = await SeedBindingAsync(WebhookPath);
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
        var binding = CalendarIntegration.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WebhookPath,
            CalendarProvider.Outlook, CalendarSyncDirection.Pull, Guid.NewGuid(), Now);
        await using var seed = _db.CreateContext(SystemTenant());
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
    public async Task DuplicateDelivery_SameExternalEventId_NoSecondProcessedReceipt()
    {
        await SeedBindingAsync(WebhookPath);
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);
        var handler = CreateHandler();

        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        // Redeliver the SAME verified callback: the intake accepts it
        // idempotently (HTTP-level) but produces no second processed receipt.
        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body, WebhookPath), CancellationToken.None))
            .Succeeded.Should().BeTrue("the intake is idempotent at the transport boundary");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .CountAsync(r => r.ExternalEventId == externalEventId && r.Status == "Processed"))
            .Should().Be(1, "exactly one processed receipt survives the duplicate delivery");
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
            .CountAsync(r => r.ExternalEventId == externalEventId && r.Status == "Processed"))
            .Should().Be(1, "exactly one processed receipt survives concurrent delivery");
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
