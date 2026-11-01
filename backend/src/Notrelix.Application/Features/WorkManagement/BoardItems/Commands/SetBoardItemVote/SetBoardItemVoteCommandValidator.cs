namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemVote;

public class SetBoardItemVoteCommandValidator : AbstractValidator<SetBoardItemVoteCommand>
{
    public SetBoardItemVoteCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
    }
}
