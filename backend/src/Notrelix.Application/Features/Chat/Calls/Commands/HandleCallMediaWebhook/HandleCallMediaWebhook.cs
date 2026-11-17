using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.Chat.Calls.Commands.HandleCallMediaWebhook;

/// <summary>
/// The media server's signed event (SPEC-CHAT-MEDIA-CALLS section 6). It carries no user session: the request is authentic only when its
/// signature over the raw body verifies, and the call (hence the tenant) is found from the room name inside the signed body, never from
/// anything the caller chooses. The signature is checked first; an unsigned, tampered, wrongly keyed or expired request is refused and
/// nothing is read or written. The work itself runs in <see cref="IChatCallMediaWebhookProcessor"/>, under the worker scope.
/// </summary>
public record HandleCallMediaWebhookCommand(string RawBody, string? Authorization)
    : ICommand<Result>, IWriteRequest, IAnonymousRequest, IGlobalRequest;

public class HandleCallMediaWebhookCommandHandler(IChatCallMediaGateway media, IChatCallMediaWebhookProcessor processor)
    : IRequestHandler<HandleCallMediaWebhookCommand, Result>
{
    public const string Rejected = "chat.calls.webhook.rejected";

    public async Task<Result> Handle(HandleCallMediaWebhookCommand request, CancellationToken cancellationToken)
    {
        var verified = media.VerifyWebhook(request.RawBody, request.Authorization);
        if (verified is null) return Result.Failure(Rejected);

        await processor.ProcessAsync(verified, request.RawBody, cancellationToken);
        return Result.Success();
    }
}

public class HandleCallMediaWebhookCommandValidator : AbstractValidator<HandleCallMediaWebhookCommand>
{
    public HandleCallMediaWebhookCommandValidator() => RuleFor(x => x.RawBody).NotEmpty();
}

/// <summary>Applies a verified event once: a repeat of the same event id changes nothing. Implemented where the worker scope lives.</summary>
public interface IChatCallMediaWebhookProcessor
{
    Task ProcessAsync(CallMediaWebhookEvent mediaEvent, string rawBody, CancellationToken cancellationToken);
}
