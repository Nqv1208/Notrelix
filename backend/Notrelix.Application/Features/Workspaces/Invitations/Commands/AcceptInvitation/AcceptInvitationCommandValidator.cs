using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
    }
}
