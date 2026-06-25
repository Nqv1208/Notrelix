using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

public class UnassignBoardItemMemberCommandValidator : AbstractValidator<UnassignBoardItemMemberCommand>
{
    public UnassignBoardItemMemberCommandValidator()
    {
        RuleFor(x => x.BoardItemId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
