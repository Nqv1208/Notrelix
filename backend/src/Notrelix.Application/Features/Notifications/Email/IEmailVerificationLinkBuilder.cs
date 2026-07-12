namespace Notrelix.Application.Features.Notifications.Email;

public interface IEmailVerificationLinkBuilder
{
    string Build(string rawToken);
}
