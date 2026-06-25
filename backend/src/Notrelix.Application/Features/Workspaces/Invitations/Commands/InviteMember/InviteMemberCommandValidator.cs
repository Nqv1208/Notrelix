using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}
