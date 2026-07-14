using Notrelix.Application.Features.Notifications.Email;
using Notrelix.Infrastructure.Configuration;

namespace Notrelix.Infrastructure.Notifications.Links;

public sealed class EmailVerificationLinkBuilder : IEmailVerificationLinkBuilder
{
    private readonly FrontendOptions _options;

    public EmailVerificationLinkBuilder(IOptions<FrontendOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return $"{_options.AppBaseUrl.ToString().TrimEnd('/')}/verify-email#token={Uri.EscapeDataString(rawToken)}";
    }
}
