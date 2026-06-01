using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Boards.RemoveBoardMember;

public class RemoveBoardMemberCommandValidator : AbstractValidator<RemoveBoardMemberCommand>
{
    public RemoveBoardMemberCommandValidator()
    {
    }
}
