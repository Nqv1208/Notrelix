using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Integrations.Public.Webhooks;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

/// <summary>
/// TAC-AI-FLOW-07 — the verified inbound calendar webhook. The raw callback
/// is signature+timestamp verified by the provider verifier before any
/// business use; tenant identity is never derived from the payload, and the
/// request authenticates by provider signature — not by a user session
/// (session-auth providers are forbidden by TAC-GATE-024).
/// </summary>
public record HandleCalendarWebhookCommand(
    string Provider,
    string Signature,
    string Timestamp,
    string RawBody) : ICommand<Result>, INoDataRequest, IAnonymousRequest, IGlobalRequest;

public class HandleCalendarWebhookCommandHandler : IRequestHandler<HandleCalendarWebhookCommand, Result>
{
    private readonly ICalendarWebhookVerifier _verifier;
    private readonly ICalendarWebhookIntake _intake;
    private readonly IDateTimeProvider _clock;

    public HandleCalendarWebhookCommandHandler(
        ICalendarWebhookVerifier verifier,
        ICalendarWebhookIntake intake,
        IDateTimeProvider clock)
    {
        _verifier = verifier;
        _intake = intake;
        _clock = clock;
    }

    public async Task<Result> Handle(HandleCalendarWebhookCommand request, CancellationToken cancellationToken)
    {
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
}
