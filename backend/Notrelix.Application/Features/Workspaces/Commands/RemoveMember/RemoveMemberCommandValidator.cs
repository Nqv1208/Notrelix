using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Commands.RemoveMember;

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
    }
}
