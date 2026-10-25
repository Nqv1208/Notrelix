namespace Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

public class FollowBoardItemCommandValidator : AbstractValidator<FollowBoardItemCommand>
{
    public FollowBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
    }
}
