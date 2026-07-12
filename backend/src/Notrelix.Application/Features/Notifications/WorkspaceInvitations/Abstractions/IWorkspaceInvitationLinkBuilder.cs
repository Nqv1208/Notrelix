namespace Notrelix.Application.Features.Notifications.WorkspaceInvitations.Abstractions;

public interface IWorkspaceInvitationLinkBuilder
{
    string Build(string rawToken);
}
