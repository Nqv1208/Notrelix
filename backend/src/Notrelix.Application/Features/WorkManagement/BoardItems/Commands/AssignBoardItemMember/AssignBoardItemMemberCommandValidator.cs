namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.AssignBoardItemMember;

public class AssignBoardItemMemberCommandValidator : AbstractValidator<AssignBoardItemMemberCommand>
{
    public AssignBoardItemMemberCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
