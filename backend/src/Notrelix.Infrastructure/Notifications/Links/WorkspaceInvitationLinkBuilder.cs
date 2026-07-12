using Notrelix.Application.Features.Notifications.WorkspaceInvitations.Abstractions;
using Notrelix.Infrastructure.Configuration;

namespace Notrelix.Infrastructure.Notifications.Links;

public sealed class WorkspaceInvitationLinkBuilder : IWorkspaceInvitationLinkBuilder
{
    private readonly FrontendOptions _options;

    public WorkspaceInvitationLinkBuilder(IOptions<FrontendOptions> options)
    {
        _options = options.Value;
    }

    public string Build(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        return $"{_options.AppBaseUrl.ToString().TrimEnd('/')}/invite#token={Uri.EscapeDataString(rawToken)}";
    }
}
