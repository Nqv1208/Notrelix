using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Webhooks;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;

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
    private readonly IDateTimeProvider _clock;

    public HandleCalendarWebhookCommandHandler(
        ICalendarWebhookBindingResolver bindingResolver,
        ICalendarWebhookVerifier verifier,
        ICalendarWebhookIntake intake,
        IDateTimeProvider clock)
    {
        _bindingResolver = bindingResolver;
        _verifier = verifier;
        _intake = intake;
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
        // identity.
        _bindingResolver.AdoptDerivedTenant(binding);

        // AI-FLOW-07 is frozen intake-only: the accepted/processed technical
        // receipt is the effect; a duplicate claim is an idempotent no-op.
        await _intake.AcceptAsync(
            request.Provider,
            verification.ExternalEventId!,
            request.RawBody,
            _clock.UtcNow,
            cancellationToken);
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
