using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Commands.CancelInvitation;

public class CancelInvitationCommandValidator : AbstractValidator<CancelInvitationCommand>
{
    public CancelInvitationCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
