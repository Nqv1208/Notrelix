namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;

public class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand>
{
    public DeclineInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
