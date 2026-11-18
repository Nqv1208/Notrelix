using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Infrastructure.Configuration;

namespace Notrelix.Infrastructure.Notifications.Links;

public sealed class SecurityPageLink : ISecurityPageLink
{
    private readonly FrontendOptions _options;

    public SecurityPageLink(IOptions<FrontendOptions> options)
    {
        _options = options.Value;
    }

    public string Build() => $"{_options.AppBaseUrl.ToString().TrimEnd('/')}/";
}
