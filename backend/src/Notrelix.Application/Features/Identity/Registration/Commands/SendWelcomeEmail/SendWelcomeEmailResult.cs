namespace Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

public sealed record SendWelcomeEmailResult(
    Guid UserId,
    string Email,
    bool AlreadySent
);
