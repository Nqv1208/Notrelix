using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
    }
}
