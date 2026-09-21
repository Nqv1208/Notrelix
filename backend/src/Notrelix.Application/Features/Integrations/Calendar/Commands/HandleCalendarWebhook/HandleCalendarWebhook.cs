using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Domain.Integrations;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

/// <summary>
/// TAC-AI-FLOW-07 — the verified inbound calendar webhook. The raw callback
/// is signature+timestamp verified by the provider verifier before any
/// business use; tenant identity is never derived from the payload, and the
/// request authenticates by provider signature — not by a user session
/// (session-auth providers are forbidden by TAC-GATE-024). The technical
/// receipt write is the protected effect of this command, so the request is
/// write-classified and executes inside the canonical data-session
/// transaction like every other write.
///
/// TAC v2.6 WAVE-E — the bootstrap claim is NON-terminal ("Captured"): the
/// handler enqueues a <see cref="CalendarWebhookProcessingRequestedV1"/>
/// provider-neutral message in the same outbox transaction, and the
/// tenant-scoped consumer decides the terminal receipt state after commit.
/// </summary>
/// <remarks>
/// C6 closure: the per-connection <c>WebhookPath</c> locator identifies the
/// owning CalendarIntegration BEFORE any payload is trusted. The binding must
/// be active, and the route <c>provider</c> must match the binding's provider.
/// Only after signature verification does the handler derive the owning
/// Account/Workspace and adopt it as the derived tenant for the receipt +
/// any downstream semantic processing. Every fail-closed branch (unknown
/// path, inactive binding, provider mismatch, failed verification) rejects
/// without adopting any tenant.
/// </remarks>
public record HandleCalendarWebhookCommand(
    string Provider,
    string Signature,
    string Timestamp,
    string RawBody,
    string WebhookPath) : ICommand<Result>, IWriteRequest, IAnonymousRequest, IGlobalRequest;

public class HandleCalendarWebhookCommandHandler : IRequestHandler<HandleCalendarWebhookCommand, Result>
{
    private readonly ICalendarWebhookBindingResolver _bindingResolver;
    private readonly ICalendarWebhookVerifier _verifier;
    private readonly ICalendarWebhookIntake _intake;
    private readonly IIntegrationEventCollector _eventCollector;
    private readonly IDateTimeProvider _clock;

    public HandleCalendarWebhookCommandHandler(
        ICalendarWebhookBindingResolver bindingResolver,
        ICalendarWebhookVerifier verifier,
        ICalendarWebhookIntake intake,
        IIntegrationEventCollector eventCollector,
        IDateTimeProvider clock)
    {
        _bindingResolver = bindingResolver;
        _verifier = verifier;
        _intake = intake;
        _eventCollector = eventCollector;
        _clock = clock;
    }

    public async Task<Result> Handle(HandleCalendarWebhookCommand request, CancellationToken cancellationToken)
    {
        // C6: the webhook path is the globally-unique locator for the owning
        // binding. It is resolved across tenants by stable identity (filters
        // bypassed, tenant-bootstrap resolver) exactly like the ResourceLocator;
        // security is provided by the signature verification below — never by
        // the path string itself.
        var binding = await _bindingResolver.ResolveActiveAsync(
            request.WebhookPath, cancellationToken);

        if (binding is null)
        {
            // Fail closed: an unknown, deactivated, or deleted webhook path is
            // rejected before any payload trust step — no tenant is ever adopted.
            return Result.Failure("integrations.webhook.rejected");
        }

        // The route segment <provider> must be the binding's provider. A
        // mismatch (or an unresolvable provider) fails closed.
        var provider = ResolveCalendarProvider(request.Provider);
        if (provider is null || provider.Value != binding.Provider)
        {
            return Result.Failure("integrations.webhook.rejected");
        }

        var verification = await _verifier.VerifyAsync(
            request.Provider, request.Signature, request.Timestamp, request.RawBody, cancellationToken);

        if (!verification.IsValid)
        {
            await _intake.RecordRejectedAsync(
                request.Provider,
                request.RawBody,
                verification.FailureReason ?? "verification failed",
                _clock.UtcNow,
                cancellationToken);
            return Result.Failure("integrations.webhook.rejected");
        }

        // Only AFTER successful provider verification does the owning tenant
        // become the derived execution context for the receipt and any
        // downstream semantic processing. The payload never supplies tenant
        // identity, and the trusted ConnectionId provenance comes from the
        // Binding resolved by WebhookPath — never from the payload.
        await _bindingResolver.AdoptDerivedTenantAsync(binding, cancellationToken);

        // AI-FLOW-07 + Wave E: the bootstrap only CLAIMS the delivery
        // (non-terminal "Captured"). A duplicate claim is an idempotent no-op
        // that enqueues nothing. Dedup is connection-scoped (connection_id,
        // provider, external_event_id) because the provider event id only
        // namespaces a delivery within a provider calendar/connection
        // (BE-API-041 provider-contract scope).
        var receivedAt = _clock.UtcNow;
        var intakeResult = await _intake.AcceptAsync(
            binding.ConnectionId,
            request.Provider,
            verification.ExternalEventId!,
            request.RawBody,
            receivedAt,
            cancellationToken);

        if (intakeResult.Outcome == CalendarWebhookIntakeOutcome.Accepted)
        {
            // Enqueue the provider-neutral processing intent in the SAME
            // outbox transaction as the Captured claim. The Workspace-scoped
            // envelope carries the derived tenant so the consumer restores it
            // on the RLS session before executing the processing seam. The
            // raw payload is NOT copied here — it is decrypted from the
            // receipt inside the tenant-scoped consumer.
            var occurredAt = _clock.UtcNow;
            _eventCollector.Add(new CalendarWebhookProcessingRequestedV1(
                EventId: Guid.CreateVersion7(),
                ReceiptId: intakeResult.ReceiptId!.Value,
                ConnectionId: binding.ConnectionId,
                Provider: request.Provider,
                ExternalEventId: verification.ExternalEventId!,
                PayloadHash: intakeResult.PayloadHash!,
                ReceivedAt: receivedAt,
                AccountIdValue: binding.AccountId,
                WorkspaceIdValue: binding.WorkspaceId,
                OccurredAt: occurredAt,
                CorrelationId: Guid.CreateVersion7()));
        }

        return Result.Success();
    }

    private static CalendarProvider? ResolveCalendarProvider(string provider)
    {
        if (Enum.TryParse<IntegrationProvider>(provider, ignoreCase: true, out var integrationProvider))
        {
            return integrationProvider switch
            {
                IntegrationProvider.Google => CalendarProvider.Google,
                IntegrationProvider.Microsoft => CalendarProvider.Outlook,
                _ => null,
            };
        }

        return null;
    }
}
