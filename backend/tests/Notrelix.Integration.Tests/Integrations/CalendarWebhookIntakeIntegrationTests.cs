using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Infrastructure.Integrations.Webhooks;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integrations;

/// <summary>
/// TAC-AI-FLOW-07 — the verified inbound webhook intake over real PostgreSQL:
/// signature+timestamp verification gates every callback; the receipt
/// deduplicates by (provider, external event id) so a duplicate delivery
/// produces no second business effect; rejected callbacks are recorded for
/// bounded diagnostics only.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarWebhookIntakeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private const string Provider = "google";
    private const string ProviderSecret = "calendar-google-webhook-secret";

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

    private HandleCalendarWebhookCommandHandler CreateHandler(FakeCurrentTenantContext? tenant = null)
    {
        var scopeTenant = tenant ?? SystemTenant();
        var context = _db.CreateContext(scopeTenant);
        return new HandleCalendarWebhookCommandHandler(
            new CalendarWebhookVerifier(new FixedClock(Now)),
            new CalendarWebhookIntake(context, new FixedClock(Now)),
            new FixedClock(Now));
    }

    [Fact]
    public async Task VerifiedCallback_IsAccepted_AndProcessed()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body), CancellationToken.None);

        result.Succeeded.Should().BeTrue("a correctly signed callback must be accepted");

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.ExternalEventId == externalEventId);
        receipt.Status.Should().Be("Processed");
    }

    [Fact]
    public async Task InvalidSignature_IsRejected_BeforeAnyBusinessEffect()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, _, timestamp) = SignedCallback(externalEventId, Now);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, "sha256=DEADBEEF", timestamp, body), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a tampered callback must be rejected");
        result.Errors.Should().Contain("integrations.webhook.rejected");

        await using var verify = _db.CreateContext(SystemTenant());
        var receipt = await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .SingleAsync(r => r.Provider == Provider && r.Status == "Rejected");
        receipt.FailureReason.Should().Contain("Signature mismatch");
        receipt.Status.Should().Be("Rejected",
            "a rejected callback must never become business processing state");
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .AnyAsync(r => r.ExternalEventId == externalEventId && r.Status == "Processed"))
            .Should().BeFalse("the rejected callback must produce no business effect");
    }

    [Fact]
    public async Task StaleTimestamp_IsRejected_AsReplay()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, _) = SignedCallback(externalEventId, Now.AddMinutes(-30));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, Now.AddMinutes(-30).ToUnixTimeSeconds().ToString(), body), CancellationToken.None);

        result.Succeeded.Should().BeFalse("a stale callback must be rejected as a replay attempt");
    }

    [Fact]
    public async Task DuplicateDelivery_SameExternalEventId_NoSecondProcessedReceipt()
    {
        var externalEventId = Guid.NewGuid().ToString();
        var (body, signature, timestamp) = SignedCallback(externalEventId, Now);
        var handler = CreateHandler();

        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body), CancellationToken.None))
            .Succeeded.Should().BeTrue();

        // Redeliver the SAME verified callback: the intake accepts it
        // idempotently (HTTP-level) but produces no second business effect.
        (await handler.Handle(
            new HandleCalendarWebhookCommand(Provider, signature, timestamp, body), CancellationToken.None))
            .Succeeded.Should().BeTrue("the intake is idempotent at the transport boundary");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.InboundWebhookReceipts.IgnoreQueryFilters()
            .CountAsync(r => r.ExternalEventId == externalEventId && r.Status == "Processed"))
            .Should().Be(1, "exactly one processed receipt survives the duplicate delivery");
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
