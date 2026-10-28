namespace Notrelix.Application.Features.Collaboration.Watchers.Commands.UnfollowBoardItem;

public class UnfollowBoardItemCommandValidator : AbstractValidator<UnfollowBoardItemCommand>
{
    public UnfollowBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
