namespace Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

public sealed class SendWelcomeEmailCommandHandler
    : IRequestHandler<SendWelcomeEmailCommand, SendWelcomeEmailResult>
{
    public Task<SendWelcomeEmailResult> Handle(
        SendWelcomeEmailCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult(new SendWelcomeEmailResult(
            request.UserId,
            request.Email,
            AlreadySent: false));
}
