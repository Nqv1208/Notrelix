namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;

public class AddBoardMemberCommandValidator : AbstractValidator<AddBoardMemberCommand>
{
    public AddBoardMemberCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
