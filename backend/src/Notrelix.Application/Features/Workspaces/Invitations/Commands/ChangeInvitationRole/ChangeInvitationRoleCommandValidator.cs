namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;

public class ChangeInvitationRoleCommandValidator : AbstractValidator<ChangeInvitationRoleCommand>
{
    public ChangeInvitationRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}
