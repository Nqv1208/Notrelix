using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;

public class RemoveBoardMemberCommandValidator : AbstractValidator<RemoveBoardMemberCommand>
{
    public RemoveBoardMemberCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
