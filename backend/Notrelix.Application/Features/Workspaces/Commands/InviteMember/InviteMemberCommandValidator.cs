using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
    }
}
