using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;

public class CancelInvitationCommandValidator : AbstractValidator<CancelInvitationCommand>
{
    public CancelInvitationCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
