using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
    }
}
